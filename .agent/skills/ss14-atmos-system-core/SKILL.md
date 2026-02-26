---
name: ss14-atmos-system-core
description: Разбирает архитектуру AtmosSystem в Space Station 14 на уровне server/shared/client: цикл обработки, состояние тайлов, инвалидации, связь с DeltaPressure и overlay-синхронизацией. Используй, когда нужно понять как система реально работает, где расширять безопасно, и как не ломать производительность/согласованность атмоса.
---

# AtmosSystem: Архитектура и Цикл

Используй этот skill как архитектурный playbook по AtmosSystem 🙂
Держи фокус на актуальном коде и проверяй свежесть через `git blame` (cutoff: `2024-02-19`).

## Что загружать в первую очередь

1. `references/fresh-pattern-catalog.md` — свежие рабочие паттерны ✅
2. `references/rejected-snippets.md` — старые/проблемные зоны, которые нельзя брать как эталон ⚠️
3. `references/docs-context.md` — вспомогательные идеи из docs (не источник истины)

## Источник правды

1. Кодовая база — основной источник истины.
2. Документация — вторичный слой для терминологии и дизайн-намерений.
3. Любой участок старше двух лет или с TODO по теме не поднимай в rules/patterns.

## Ментальная модель AtmosSystem

1. Система живет вокруг `GridAtmosphereComponent`: у каждой сетки есть набор `TileAtmosphere`, очереди текущего цикла и processing-state.
2. Обработка идет стадиями через конечный автомат: `Revalidate -> TileEqualize -> ActiveTiles -> ExcitedGroups -> HighPressureDelta -> DeltaPressure -> Hotspots -> Superconductivity -> PipeNet -> AtmosDevices`.
3. Каждая стадия обязана уметь паузиться по time-budget и продолжаться в следующем тике.
4. Любое изменение геометрии/airtight сначала инвалидирует тайл, а реальная переоценка происходит на стадии `Revalidate`.
5. Получение газа строится по цепочке `grid tile -> map atmosphere -> SpaceGas fallback`.
6. Визуалы газа отделены от физики газа: физика меняет данные тайла, а overlay получает инвалидированные чанки и синхронизируется сетью.
7. DeltaPressure — отдельный subsystem внутри цикла: параллельный сбор давлений + отложенное применение урона.

## Паттерны

1. Реализуй новую механику как отдельный этап или как отдельный блок в существующем этапе с явной паузой по таймеру.
2. После изменения airtight/тайла всегда вызывай инвалидацию тайла, а не пересчитывай все вручную.
3. Для чтения блокировок воздуха в gameplay-коде предпочитай cached-вариант проверки.
4. Для map-level атмосферы используй immutable смесь и централизованный refresh map-atmos тайлов.
5. Для сущностей с баротравмой используй lifecycle регистрации в DeltaPressure-списках (init/shutdown/grid changed).
6. Для shared-логики дыхания используй события маски/интерналов и поддерживай корректный disconnect.
7. Для клиентского газа объединяй full/delta state через единый merge-проход по чанкам.

## Анти-паттерны

1. Считать, что Atmos обновляет все сразу за один тик; игнорировать `ProcessingPaused`.
2. Обходить инвалидацию и менять кэши airtight/tiles «по месту».
3. Использовать устаревшие зоны с TODO как reference-реализацию.
4. Привязывать логику к порядку вызова устройств как к «фиче».
5. Писать тяжелую логику без yield-точек по time-budget.
6. Рассчитывать, что docs всегда совпадают с текущим поведением кода.

## Способы оптимизации при работе с системой

1. Для частых gameplay-проверок используй `IsTileAirBlockedCached(...)`, а не on-the-fly вариант.
2. Для набора тайлов используй `GetTileMixtures(...)` вместо многократных одиночных вызовов.
3. Ставь `excite: true` только когда действительно нужен немедленный отклик атмоса/визуалов.
4. После локальных изменений вызывай `InvalidateTile(...)`, не запускай ручной массовый пересчет.
5. В DeltaPressure подбирай `DeltaPressureParallelProcessPerIteration` и `DeltaPressureParallelBatchSize` под нагрузку сервера.
6. Не dirty/refresh весь grid overlay без необходимости, инвалидируй только затронутые тайлы.
7. В новых подсистемах делай обработку порционной: очередь + периодическая проверка time-budget.
8. Если нужен быстрый pre-filter точки, комбинируй `IsTileSpace(...)` + `IsTileAirBlockedCached(...)`.
9. Для map-level изменений обновляй атмосферу пакетно (`SetMapAtmosphere/SetMapGasMixture/SetMapSpace`), а не по тайлу.

## Примеры из кода

### 1) Конечный автомат стадий атмоса

```csharp
// Каждая стадия возвращает: продолжать, перейти к следующей или приостановить цикл.
switch (atmosphere.State)
{
    case AtmosphereProcessingState.Revalidate:
        if (!ProcessRevalidate(ent))
            return AtmosphereProcessingCompletionState.Return; // time-budget exceeded

        atmosphere.State = MonstermosEqualization
            ? AtmosphereProcessingState.TileEqualize
            : AtmosphereProcessingState.ActiveTiles;
        return AtmosphereProcessingCompletionState.Continue;

    case AtmosphereProcessingState.DeltaPressure:
        if (!ProcessDeltaPressure(ent))
            return AtmosphereProcessingCompletionState.Return;

        atmosphere.State = AtmosphereProcessingState.Hotspots;
        return AtmosphereProcessingCompletionState.Continue;
}
```

### 2) Канал инвалидации: изменение airtight -> revalidate

```csharp
// При изменении позиции/состояния airtight-энтити помечай тайл как invalid.
public void InvalidatePosition(Entity<MapGridComponent?> grid, Vector2i pos)
{
    _explosionSystem.UpdateAirtightMap(grid, pos, grid);
    _atmosphereSystem.InvalidateTile(grid.Owner, pos);
}

// На стадии Revalidate тайл пересобирает TileData/AirtightData и визуалы.
UpdateTileData(ent, mapAtmos, tile);
UpdateAdjacentTiles(ent, tile, activate: true);
UpdateTileAir(ent, tile, volume);
InvalidateVisuals(ent, tile);
```

### 3) DeltaPressure: параллельный расчет + отложенный урон

```csharp
// Расчет давления выполняется батчами в parallel-job.
var job = new DeltaPressureParallelJob(this, atmosphere, atmosphere.DeltaPressureCursor, DeltaPressureParallelBatchSize);
_parallel.ProcessNow(job, toProcess);

// Сам урон применяется отдельным проходом из очереди результатов.
while (atmosphere.DeltaPressureDamageResults.TryDequeue(out var result))
{
    PerformDamage(result.Ent, result.Pressure, result.DeltaPressure);
}
```

### 4) Shared: интеграция дыхательной маски и интерналов

```csharp
// При выключении маски пробуем подключить дыхательный инструмент к интерналам носителя.
private void OnMaskToggled(Entity<BreathToolComponent> ent, ref ItemMaskToggledEvent args)
{
    if (args.Mask.Comp.IsToggled)
    {
        DisconnectInternals(ent, forced: true);
    }
    else if (_internalsQuery.TryComp(args.Wearer, out var internals))
    {
        _internals.ConnectBreathTool((args.Wearer.Value, internals), ent);
    }
}
```

### 5) Client: корректный merge full/delta gas-overlay чанков

```csharp
// Клиент принимает либо full state, либо delta state, но применяет одинаково через modifiedChunks.
switch (args.Current)
{
    case GasTileOverlayDeltaState delta:
        modifiedChunks = delta.ModifiedChunks;
        // Удаляем локальные чанки, которых больше нет в серверном списке.
        break;
    case GasTileOverlayState state:
        modifiedChunks = state.Chunks;
        break;
}

foreach (var (index, data) in modifiedChunks)
{
    comp.Chunks[index] = data; // единый путь применения
}
```

## Правило расширения

1. Сначала расширяй свежие этапы и свежие API-поверхности.
2. Если нужно менять legacy-блок, сначала зафиксируй риск в `references/rejected-snippets.md` и только потом вноси изменение.
3. Любой новый subsystem делай конфигурируемым и паузируемым.
4. Любое «ускорение» проверяй метриками (время кадра/атмотика), а не интуицией.

Думай как о конвейере с очередями, а не как о «одной функции атмоса» 🧪
