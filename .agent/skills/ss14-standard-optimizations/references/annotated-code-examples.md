# Аннотированные примеры из кода

Ниже примеры, сверенные по реальному коду.  
Формат: подсистема, сигнатура, слой, актуальность, затем фрагмент с пояснениями.

## Пример 1: предвычисление до вложенных циклов (real fast-path)

- Подсистема: storage area insert
- Сигнатура: участок подбора позиции/угла в методе проверки размещения
- Слой: `shared`
- Актуальность: `2025-05`

```csharp
var itemShape = ItemSystem.GetItemShape(itemEnt); // Один расчёт формы до циклов.
var fastAngles = itemShape.Count == 1;
var fastPath = itemShape.Count == 1 && itemShape[0].Contains(Vector2i.Zero);

var angles = new ValueList<Angle>();
if (!fastAngles)
{
    for (var angle = startAngle; angle <= Angle.FromDegrees(360 - startAngle); angle += Math.PI / 2f)
        angles.Add(angle); // Подготовили набор углов один раз.
}
else
{
    angles.Add(startAngle);
    if (itemShape[0].Width != itemShape[0].Height)
        angles.Add(startAngle + Angle.FromDegrees(90));
}

while (chunkEnumerator.MoveNext(out var storageChunk))
{
    for (var y = bottom; y <= top; y++)
    {
        for (var x = left; x <= right; x++)
        {
            foreach (var angle in angles)
            {
                // Внутри самого дорогого участка используем только предвычисленные данные.
                // Это убирает повторный расчёт формы/углов на каждый тайл.
            }
        }
    }
}
```

## Пример 2: `fieldDeltas + DirtyField` для точечных изменений

- Подсистема: proximity detector
- Сигнатура: `public override void Update(float frameTime)`
- Слой: `shared`
- Актуальность: `2025-05`

```csharp
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class ProximityDetectorComponent : Component
{
    [AutoNetworkedField] public TimeSpan NextUpdate = TimeSpan.Zero;
    [AutoNetworkedField] public float Distance = float.PositiveInfinity;
    [AutoNetworkedField] public EntityUid? Target;
}

public override void Update(float frameTime)
{
    var query = EntityQueryEnumerator<ProximityDetectorComponent>();
    while (query.MoveNext(out var uid, out var component))
    {
        if (component.NextUpdate > _timing.CurTime)
            continue;

        component.NextUpdate += component.UpdateCooldown;
        DirtyField(uid, component, nameof(ProximityDetectorComponent.NextUpdate));
        // Грязним только реально изменённое поле.
    }
}
```

## Пример 3: кеш `EntityQuery<T>` в `Initialize()`

- Подсистема: proximity detector
- Сигнатура: `public override void Initialize()`
- Слой: `shared`
- Актуальность: `2025-05`

```csharp
private EntityQuery<TransformComponent> _xformQuery;

public override void Initialize()
{
    SubscribeLocalEvent<ProximityDetectorComponent, MapInitEvent>(OnMapInit);
    _xformQuery = GetEntityQuery<TransformComponent>(); // Кешируем один раз.
}

private void UpdateTarget(Entity<ProximityDetectorComponent> detector)
{
    if (!_xformQuery.TryGetComponent(detector, out var transform))
        return; // Быстрый ранний выход.

    // ... дальнейшая логика поиска цели ...
}
```

## Пример 4: `ByRef record struct` для частого локального события

- Подсистема: power charge machine
- Сигнатура: `[ByRefEvent] public record struct ...`
- Слой: `server`
- Актуальность: `2024-08`

```csharp
[ByRefEvent] public record struct ChargedMachineActivatedEvent;
[ByRefEvent] public record struct ChargedMachineDeactivatedEvent;

private void Notify(EntityUid uid, bool active)
{
    if (active)
    {
        var ev = new ChargedMachineActivatedEvent();
        RaiseLocalEvent(uid, ref ev); // By-ref вызов.
        return;
    }

    var off = new ChargedMachineDeactivatedEvent();
    RaiseLocalEvent(uid, ref off);
}
```

## Пример 5: переиспользование `ValueList` без пер-кадровых аллокаций

- Подсистема: визуальные эффекты
- Сигнатура: `public override void Update(float frameTime)`
- Слой: `client`
- Актуальность: `2024-06`

```csharp
private ValueList<EntityUid> _toRemove = new();

public override void Update(float frameTime)
{
    var query = AllEntityQuery<ColorFlashEffectComponent>();
    _toRemove.Clear(); // Очистка вместо new ValueList<...>().

    while (query.MoveNext(out var uid, out _))
    {
        if (_animation.HasRunningAnimation(uid, AnimationKey))
            continue;

        _toRemove.Add(uid);
    }

    foreach (var ent in _toRemove)
        RemComp<ColorFlashEffectComponent>(ent);
}
```

## Пример 6: `ArrayPool<T>` + `ReadOnlySpan<T>` при рассылке

- Подсистема: device network
- Сигнатура: `private void SendPacket(...)` / `private void SendToConnections(...)`
- Слой: `server`
- Актуальность: `2022-05` (soft-exception: каноничный паттерн, до сих пор применим)

```csharp
var deviceCopy = ArrayPool<DeviceNetworkComponent>.Shared.Rent(totalDevices);
try
{
    devices.CopyTo(deviceCopy);
    SendToConnections(deviceCopy.AsSpan(0, totalDevices), packet);
}
finally
{
    ArrayPool<DeviceNetworkComponent>.Shared.Return(deviceCopy);
}

private void SendToConnections(ReadOnlySpan<DeviceNetworkComponent> connections, DeviceNetworkPacketEvent packet)
{
    foreach (var connection in connections)
    {
        // Работаем со span без лишних копий списка.
    }
}
```

## Пример 7: `EntityQuery<T>` в API системы вместо повторных общих проверок

- Подсистема: tags
- Сигнатура: `public override void Initialize()` / `public bool HasTag(...)`
- Слой: `shared`
- Актуальность: `2024-05`

```csharp
private EntityQuery<TagComponent> _tagQuery;

public override void Initialize()
{
    _tagQuery = GetEntityQuery<TagComponent>();
}

public bool HasTag(EntityUid uid, ProtoId<TagPrototype> tag)
{
    return _tagQuery.TryComp(uid, out var component) &&
           component.Tags.Contains(tag);
}
```

## Пример 8: порядок компонентов в query по кардинальности

- Подсистема: ECS runtime
- Сигнатура: `EntityQuery<TComp1, TComp2>(...)`
- Слой: `engine`
- Актуальность: `2023-11` (soft-exception: каноничный комментарий движка)

```csharp
// В runtime есть прямое указание:
// "you really want trait1 to be the smaller set of components"

// Практический вывод:
var query = EntityQueryEnumerator<ActiveTimerTriggerComponent, TimerTriggerComponent>();
// Сначала более редкий компонент, чтобы сократить пересечение.
```

## Пример 9: инкрементальный счётчик вместо пересчёта состояния

- Подсистема: ranged gun burst mode
- Сигнатура: участок обработки выстрела в `SharedGunSystem`
- Слой: `shared`
- Актуальность: `2024-10` (используется в актуальном коде 2025+)

```csharp
if (gun.SelectedMode == SelectiveFire.Burst)
    gun.BurstActivated = true;

if (gun.BurstActivated)
{
    gun.BurstShotsCount += shots; // Поддерживаем агрегат инкрементально.

    if (gun.BurstShotsCount >= gun.ShotsPerBurstModified)
    {
        gun.NextFire += TimeSpan.FromSeconds(gun.BurstCooldown);
        gun.BurstActivated = false;
        gun.BurstShotsCount = 0; // Сброс при завершении burst-окна.
    }
}
```

Комментарий: система не «пересчитывает историю выстрелов» для проверки лимита burst, а обновляет счётчик по месту изменения состояния.
