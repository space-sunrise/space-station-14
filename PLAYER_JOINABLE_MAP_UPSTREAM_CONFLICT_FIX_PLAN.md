# План устранения upstream-конфликтов PR Player Joinable Maps

## 1. Цель

Снизить только ту конфликтную поверхность с официальным `space-wizards/space-station-14`,
которую добавляет или изменяет PR `origin/master..HEAD`.

План не включает исправление старых конфликтов Sunrise-форка, существовавших до PR.
Несвязанные upstream-maintenance рефакторинги следует вынести из feature-PR в отдельный PR.

## 2. Зафиксированная база анализа

- База PR: `origin/master` (`a2dc3d6116caea8347370d3a07c4853a91b873d3`).
- Анализируемый HEAD: `448163caea74e161eef7cfdbae15a053792be976`.
- Официальный upstream snapshot: `68ec73bea63b7e2257a4bd023b4ac59b036b6d08`.
- Виртуальное слияние базы PR с upstream: tree `02d7f7006bdd32719ba8aeb28211b0ebb9ad06e6`.
- Виртуальное слияние HEAD с upstream: tree `83df4942e3380d70a3d4bd7289b5369d125be45e`.

Метрики ниже относятся к конфликтным блокам виртуального merge, а не к обычному размеру diff.

| Файл | До PR | После PR | Ответственность PR |
| --- | ---: | ---: | --- |
| `HumanoidProfileEditor.xaml.cs` | 10 hunks / 1504 строки | 10 hunks / 1565 строк | Изменены 3 старых hunk, `+61` строк конфликтной области |
| `JoinGameCommand.cs` | 0 | 1 hunk / 34 строки | Новый конфликтный файл |
| `GameTicker.Spawning.cs` | 2 hunks / 23 строки | 3 hunks / 28 строк | Один новый hunk, `+5` строк |
| `StationJobsSystem.Roundstart.cs` | 1 hunk / 9 строк | 0 | Удалён старый конфликт, но рефакторинг не относится к feature |
| `StationSpawningSystem.cs` | 3 hunks / 47 строк | 3 hunks / 20 строк | Изменены старые конфликты несвязанным cleanup |
| `HumanoidCharacterProfile.cs` | 7 hunks / 190 строк | 7 hunks / 190 строк | Все конфликтные блоки идентичны; действий не требуется |
| `JobPrototype.cs` | 2 hunks / 177 строк | 2 hunks / 141 строк | Изменены старые конфликты несвязанным cleanup |

## 3. Границы задачи

### Входит в план

- Новый конфликт в `JoinGameCommand.cs`.
- Дополнительный конфликтный hunk в `GameTicker.Spawning.cs`.
- Три изменённых PR конфликтных блока в `HumanoidProfileEditor.xaml.cs`:
  - fork-specific `using`;
  - поля состояния Player Joinable Maps;
  - переработка `RefreshJobs()`.
- Удаление из feature-PR несвязанных изменений старых конфликтных областей.
- Повторная проверка виртуального merge до и после исправлений.

### Не входит в план

- Семь старых и не изменённых PR конфликтов `HumanoidCharacterProfile.cs`.
- `SpawnPointSystem.cs`, `StationJobsSystem.cs`, FTL и `_Sunrise` файлы, которые не создают
  конфликтов с зафиксированным upstream snapshot.
- Общее обновление Sunrise до актуального upstream.
- Исправление старых конфликтов всего форка.
- Миграция на новые upstream API, если она не нужна для устранения конфликтной дельты PR.

## 4. Обязательные навыки для реализации

- `ss14-upstream-maintenance` — минимальные hooks в vanilla и изоляция в `_Sunrise`.
- `ss14-naming-conventions` — имена partial-файлов, методов и зависимостей.
- `SS14 ECS Components`.
- `SS14 ECS Entities`.
- `SS14 ECS Prototypes`.
- `SS14 ECS Systems`.
- `ss14-events`.
- `SS14 Prediction`.
- `SS14 UI XAML` — изменение логики редактора персонажа.
- `ss14-documentation-writing` — документация новых API и partial-точек.
- `SS14 Tests Authoring` — сохранение поведения после декомпозиции.

## 5. Целевое состояние

После исправлений feature-PR должен удовлетворять следующим условиям:

1. `JoinGameCommand.cs`: `0 -> 0` конфликтных hunks.
2. `GameTicker.Spawning.cs`: не больше двух старых hunks; PR не добавляет третий.
3. `HumanoidProfileEditor.xaml.cs`:
   - нет fork-specific полей и больших helper-методов в vanilla;
   - нет прямого `using Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps`;
   - изменение `RefreshJobs()` сведено к минимальным маркированным hooks;
   - PR не создаёт новых конфликтных hunks;
   - допустим максимум один изменённый старый hunk, если без него невозможно вывести отдельные секции карт.
4. `StationSpawningSystem.cs`, `JobPrototype.cs` и `StationJobsSystem.Roundstart.cs` не меняются
   feature-PR ради несвязанного cleanup.
5. Вся новая реализация остаётся в `_Sunrise`; в vanilla остаются только необходимые точки подключения.
6. Игровое поведение Player Joinable Maps не меняется.

## 6. Этап 1 — удалить несвязанный cleanup из feature-PR

Цель этапа — не улучшать метрики PR за счёт переписывания старых конфликтов и не смешивать
Player Joinable Maps с общим upstream-maintenance.

### 6.1. `StationSpawningSystem`

- [ ] Вернуть `Content.Server/Station/Systems/StationSpawningSystem.cs` к состоянию базы PR
  для всех изменений, не требуемых Player Joinable Maps.
- [ ] Убрать из feature-PR перенос sponsor/loadout/flavor-text логики в
  `Content.Server/_Sunrise/Station/Systems/StationSpawningSystem.Sponsors.cs`.
- [ ] Если этот cleanup всё ещё нужен, перенести его отдельным коммитом/PR после feature-PR.
- [ ] Подтвердить, что Player Joinable Maps не вызывает методы этого partial-файла напрямую.

Ожидаемый результат: PR перестаёт изменять три старых конфликтных блока
`StationSpawningSystem.cs`.

### 6.2. `JobPrototype`

- [ ] Вернуть `Content.Shared/Roles/JobPrototype.cs` к состоянию базы PR.
- [ ] Убрать из feature-PR перенос старых Sunrise-полей в
  `Content.Shared/_Sunrise/Roles/JobPrototype.Sunrise.cs`.
- [ ] Сохранить использование уже существующего в базе `AlwaysUseSpawner`.
- [ ] Вынести partial-миграцию `JobPrototype` в отдельный upstream-maintenance PR.

Ожидаемый результат: два конфликта `JobPrototype.cs` снова становятся полностью
предшествующими PR и не входят в его зону ответственности.

### 6.3. Spawn lifecycle и sponsor roundstart cleanup

- [ ] Отделить перенос старой spawn-логики в `SunrisePlayerSpawnSystem` от необходимых
  Player Joinable Maps hooks.
- [ ] Вернуть старые NewLife, join announcement, antag target и accent реакции туда, где они
  находились в базе PR, если их перенос не требуется для работы feature.
- [ ] Убрать из feature-PR sponsor-only переработку
  `StationJobsSystem.Roundstart.cs` и `StationJobsSystem.Sponsors.cs`.
- [ ] Перенести оба cleanup в отдельный maintenance PR.

Важно: устранённый конфликт `StationJobsSystem.Roundstart.cs` нельзя использовать как
компенсацию нового конфликта `JoinGameCommand.cs`, поскольку изменения относятся к разным задачам.

## 7. Этап 2 — убрать новый конфликт `JoinGameCommand.cs`

Текущий конфликт создаёт блок `BeforeJoinGameCommand(...)` перед штатной проверкой слотов.

### Предпочтительное решение

- [ ] Удалить изменение `Content.Server/GameTicking/Commands/JoinGameCommand.cs` из PR.
- [ ] Удалить `Content.Server/_Sunrise/GameTicking/Commands/JoinGameCommand.JoinGate.cs`,
  если после этого он не используется.
- [ ] Оставить авторитетную проверку в `GameTicker.TryPreparePlayerJoinableMapJoin()` через уже
  существующий spawn hook `ResolveDirectSpawnStationPortal`.
- [ ] Проверить, что обычный latejoin UI всегда отправляет фактическую station, для которой
  показывалась выбранная профессия.
- [ ] Для ручной команды `joingame` сохранить штатную семантику: пользователь команды обязан
  передать station с доступным job slot; автоматический поиск другой station не обязателен.

### Если ранняя проверка команды действительно обязательна

- [ ] Сначала документировать сценарий, который невозможно обработать в `GameTicker`.
- [ ] Оставить в vanilla только один маркированный вызов без объявления partial-метода и без
  дополнительного `using`.
- [ ] Объявление и реализацию метода разместить в `_Sunrise` partial-файле.
- [ ] Не рефакторить соседний парсинг аргументов и штатную slot-проверку.

Критерий приёмки этапа: виртуальный merge `JoinGameCommand.cs` содержит ноль конфликтов,
либо сохраняется один минимальный конфликт только при наличии зафиксированного обязательного сценария.

## 8. Этап 3 — минимизировать `GameTicker.Spawning.cs`

### 8.1. Убрать несвязанные изменения конфликтных блоков

- [ ] Восстановить порядок и состав `using` из базы PR, если их изменение было только cleanup.
- [ ] Восстановить исходный блок зависимостей из базы PR.
- [ ] Перенести объявления следующих partial-методов в соответствующие `_Sunrise` partial-файлы:
  - `FilterFallbackSpawnableStationsPortal`;
  - `ResolveDirectSpawnStationPortal`;
  - `FilterCanBeAntagPortal`;
  - `SelectSpawnPointTypePortal`.
- [ ] Не размещать объявления portal-методов рядом с vanilla dependencies.

### 8.2. Устранить третий конфликтный hunk

- [ ] Вернуть штатную форму вызова `DoSpawn(..., out var mob, out var jobPrototype, out var jobName, ...)`
  вместо отдельных предварительных объявлений `EntityUid`, `JobPrototype` и `string`.
- [ ] Не менять соседний контрольный поток, если это не требуется сигнатурой.

### 8.3. Оставить только feature-hooks

- [ ] Оставить минимальные маркированные вызовы для:
  - фильтрации fallback stations;
  - проверки/разрешения station перед спавном;
  - запрета antag на специальной station;
  - выбора типа spawnpoint.
- [ ] Каждый hook окружить `Sunrise added start/end` с русской reason-фразой.
- [ ] Не переносить в этот PR старую spawn lifecycle логику.

Критерий приёмки этапа: `GameTicker.Spawning.cs` не получает нового третьего conflict hunk,
а оставшиеся конфликты совпадают с конфликтами базы PR либо отличаются только минимальным feature-hook.

## 9. Этап 4 — изолировать `HumanoidProfileEditor`

Это основной конфликтный участок PR.

### 9.1. Новый partial-файл

- [ ] Создать
  `Content.Client/_Sunrise/Lobby/UI/HumanoidProfileEditor.PlayerJoinableMaps.cs`.
- [ ] Использовать namespace `Content.Client.Lobby.UI` с `IDE0130` suppression.
- [ ] Перенести туда:
  - `_playerJoinableMapBoolCVarHandlers`;
  - `_playerJoinableMapIntCVarHandlers`;
  - `_playerJoinableMapIndex`;
  - `_availablePlayerJoinableMaps`;
  - `_availablePlayerJoinableMapJobs`;
  - `_lastPlayerCount`;
  - подписку и отписку от CVar;
  - обработчики player count и prototype reload;
  - расчёт доступных карт и профессий;
  - отрисовку дополнительных секций карт.

После переноса vanilla-файл не должен импортировать namespace Player Joinable Maps.

### 9.2. Lifecycle hooks

- [ ] В constructor оставить один маркированный вызов, например
  `InitializePlayerJoinableMapsPortal()`.
- [ ] В `Dispose()` оставить один маркированный вызов
  `ShutdownPlayerJoinableMapsPortal()`.
- [ ] Объявления и реализации методов держать в `_Sunrise` partial-файле.
- [ ] Не добавлять fork-specific поля рядом с vanilla-полями.

### 9.3. Вернуть штатную структуру `RefreshJobs()`

- [ ] Восстановить тело `RefreshJobs()` максимально близко к базе PR.
- [ ] Не оставлять в vanilla локальные функции `AddSectionTitle` и `AddDepartmentJobs`, добавленные PR.
- [ ] Обычные department/job строки строить штатным кодом.
- [ ] Перед выводом обычных jobs добавить минимальный portal-фильтр, который исключает профессии,
  принадлежащие доступным/зарезервированным Player Joinable Maps.
- [ ] После штатных departments вызвать portal, добавляющий дополнительные map sections.
- [ ] Fork partial может дублировать небольшой участок построения job row, если это позволяет не
  переписывать большой vanilla-метод. Для upstream-friendly это предпочтительнее общей переработки.

Рекомендуемые сигнатуры hooks необходимо уточнить по фактическим локальным типам, например:

```csharp
partial void FilterPlayerJoinableMapJobsPortal(
    DepartmentPrototype department,
    List<JobPrototype> jobs);

partial void AddPlayerJoinableMapSectionsPortal(
    IReadOnlyList<(string LocKey, int Priority)> priorityItems,
    ref bool firstCategory);
```

Сигнатуры являются направлением, а не обязательным API: нельзя ради них расширять vanilla diff.

### 9.4. Ограничение UI-конфликтов

- [ ] Удалить PR-изменения из конфликтного блока `using`.
- [ ] Удалить PR-изменения из конфликтного блока полей.
- [ ] Свести изменения большого конфликтного блока `RefreshJobs()` к двум коротким hooks.
- [ ] Не переносить и не форматировать соседний Sunrise UI-код в рамках этого PR.

Критерий приёмки этапа: из трёх изменённых PR старых конфликтных блоков остаётся не более одного,
и его PR-часть состоит только из минимальных маркированных вызовов.

## 10. Этап 5 — статическая проверка diff

- [ ] Проверить рабочее дерево и убедиться, что нет случайных файлов:

```powershell
git status --short
```

- [ ] Проверить итоговый список vanilla-файлов PR:

```powershell
git diff --name-status origin/master..HEAD -- Content.Client Content.Server Content.Shared
```

- [ ] Проверить отсутствие whitespace-ошибок:

```powershell
git diff --check origin/master..HEAD
```

- [ ] Проверить, что несвязанные конфликтные файлы исключены или содержат только необходимые feature-hooks:

```powershell
git diff origin/master..HEAD -- `
  Content.Server/Station/Systems/StationSpawningSystem.cs `
  Content.Server/Station/Systems/StationJobsSystem.Roundstart.cs `
  Content.Shared/Roles/JobPrototype.cs
```

## 11. Этап 6 — повторная конфликтная проверка

Использовать тот же pinned upstream commit, иначе метрики до/после будут несопоставимы.

- [ ] Построить виртуальный merge базы PR:

```powershell
git merge-tree --write-tree --name-only origin/master 68ec73bea63b7e2257a4bd023b4ac59b036b6d08
```

- [ ] Построить виртуальный merge исправленного HEAD:

```powershell
git merge-tree --write-tree --name-only HEAD 68ec73bea63b7e2257a4bd023b4ac59b036b6d08
```

- [ ] Для каждого vanilla-файла PR сравнить:
  - количество `<<<<<<<`;
  - состав конфликтных блоков после нормализации marker labels;
  - число строк внутри конфликтных блоков.
- [ ] Зафиксировать итог в PR-описании отдельной таблицей `before PR / after PR`.

### Минимальный conflict budget

| Файл | Максимально допустимый результат |
| --- | --- |
| `JoinGameCommand.cs` | 0 новых hunks |
| `GameTicker.Spawning.cs` | 0 новых hunks относительно базы PR |
| `HumanoidProfileEditor.xaml.cs` | 0 новых hunks; не более 1 изменённого старого hunk |
| `StationSpawningSystem.cs` | 0 конфликтных изменений в зоне ответственности PR |
| `StationJobsSystem.Roundstart.cs` | 0 конфликтных изменений в зоне ответственности PR |
| `JobPrototype.cs` | 0 конфликтных изменений в зоне ответственности PR |
| `HumanoidCharacterProfile.cs` | Не учитывать: конфликты идентичны до/после PR |

## 12. Этап 7 — сборка и поведенческая проверка

Запускать команды последовательно.

- [ ] Собрать Shared:

```powershell
dotnet build Content.Shared/Content.Shared.csproj --configuration Debug
```

- [ ] Собрать Server:

```powershell
dotnet build Content.Server/Content.Server.csproj --configuration Debug
```

- [ ] Собрать Client:

```powershell
dotnet build Content.Client/Content.Client.csproj --configuration Debug
```

- [ ] Запустить релевантные тесты Player Joinable Maps, если они присутствуют в итоговой ветке.
- [ ] Проверить вручную:
  - disabled map не показывает jobs и не принимает latejoin;
  - player-count gated map обновляет список jobs после изменения числа игроков;
  - job со свободным slot появляется только у допустимой station;
  - job spawnpoint используется для настроенной карты;
  - обычные station jobs и обычный latejoin не изменились;
  - ручная команда `joingame` не обходит серверную проверку.
- [ ] Запустить клиент и завершить процесс после проверки:

```powershell
dotnet run --project Content.Client/Content.Client.csproj
```

## 13. Порядок коммитов

Рекомендуемая структура истории после исправлений:

1. `Player joinable maps: isolate profile editor integration`
2. `Player joinable maps: minimize spawning and latejoin hooks`
3. `Player joinable maps: add conflict regression verification/tests`

Не включать в эти коммиты:

- общий sponsor cleanup;
- перенос старого spawn lifecycle;
- общий partial-рефакторинг `JobPrototype`;
- иные исправления старых upstream-конфликтов Sunrise.

## 14. Definition of Done

- [ ] Feature-PR не добавляет новый конфликтный файл `JoinGameCommand.cs`.
- [ ] Feature-PR не добавляет третий конфликтный hunk `GameTicker.Spawning.cs`.
- [ ] В `HumanoidProfileEditor.xaml.cs` отсутствуют fork-specific поля и крупная fork-реализация.
- [ ] Все новые реализации находятся в `_Sunrise`.
- [ ] Несвязанные cleanup вынесены из feature-PR.
- [ ] Conflict budget из раздела 11 выполнен.
- [ ] `git diff --check` проходит.
- [ ] Shared, Server и Client собираются.
- [ ] Клиент запущен для runtime-проверки и затем остановлен.
- [ ] Поведение обычного roundstart и latejoin не изменилось.
