# Architecture and Runtime Map

## Назначение

Используй этот файл, когда нужно быстро восстановить в голове, как именно работает NPC runtime в SS14/Sunrise.

## Карта слоев

1. Оркестрация NPC:
`Content.Server/NPC/Systems/NPCSystem.cs`
2. HTN runtime:
`Content.Server/NPC/HTN/HTNSystem.cs`
3. Планировщик:
`Content.Server/NPC/HTN/HTNPlanJob.cs`
4. Данные плана:
`Content.Server/NPC/HTN/HTNPlan.cs`
5. Базовые контракты tasks:
`Content.Server/NPC/HTN/HTNTask.cs`
`Content.Server/NPC/HTN/HTNCompoundTask.cs`
`Content.Server/NPC/HTN/PrimitiveTasks/HTNPrimitiveTask.cs`
6. Контракты расширения:
`Content.Server/NPC/HTN/PrimitiveTasks/HTNOperator.cs`
`Content.Server/NPC/HTN/Preconditions/HTNPrecondition.cs`
`Content.Server/NPC/HTN/IHtnConditionalShutdown.cs`
7. Blackboard:
`Content.Server/NPC/NPCBlackboard.cs`
`Content.Server/NPC/NPCBlackboardSerializer.cs`
8. Навигация и steering:
`Content.Server/NPC/Systems/NPCSteeringSystem.cs`
9. Utility-оценка целей:
`Content.Server/NPC/Systems/NPCUtilitySystem.cs`
`Content.Server/NPC/Queries/UtilityQueryPrototype.cs`
10. Фракции:
`Content.Shared/NPC/Systems/NpcFactionSystem.cs`

## Цикл жизни NPC (runtime)

1. На `MapInit` NPC “будится” (`WakeNPC`) и получает `ActiveNPCComponent`.
2. `NPCSystem.Update()` вызывает `HTNSystem.UpdateNPC(...)` в рамках бюджетных лимитов.
3. `HTNSystem`:
обрабатывает очередь планирования -> подхватывает готовый plan job -> при необходимости подменяет текущий план -> выполняет `Update()` текущего оператора.
4. Если оператор завершился:
выполняется shutdown, индекс плана сдвигается, запускается следующий task.
5. Если оператор упал:
текущий план гасится и инициируется новый цикл планирования.
6. Если entity умер/выключен/занят игроком:
NPC переводится в sleep, активные компоненты поведения снимаются.

## Планирование и execution: строгая граница

1. Планирование:
выполняется асинхронно (job queue), проверяет preconditions и `Plan()`.
2. Execution:
выполняется в обычном апдейте мира через `Startup()`/`Update()`/shutdown.
3. Effects:
данные, возвращенные из `Plan()`, могут быть применены на startup шага (reuse результатов планирования).

## HTN-декомпозиция: как выбирается ветка

1. Для `HTNCompoundTask` перебираются branches сверху вниз.
2. Первая ветка, где все branch-preconditions истинны, попадает в стек декомпозиции.
3. Если далее план развалился на дочернем шаге, планировщик откатывает состояние (blackboard + selected primitives) и пробует следующую ветку.
4. Если все ветки провалены, текущая compound-точка считается неразрешимой.

## Blackboard-модель

1. Blackboard хранит состояние NPC и настраиваемые параметры.
2. Ключи строковые, есть базовые константы (`Owner`, `Target`, `MovementPathfind`, `NavInteract` и др.).
3. Есть дефолты (например, `VisionRadius`, `MeleeRange`, `FollowRange`), которые подхватываются даже без явной записи.
4. Во время планирования доска может работать в read-only режиме.
5. Для YAML blackboard используется типизированная сериализация (`!type:Single`, `!type:Bool`, `!type:SoundPathSpecifier` и т.п.).

## Сервисные подсистемы, которые обычно участвуют в поведении

1. `NPCSteeringSystem`:
регистрация movement-задачи, pathfinding, обход препятствий, завершение по range/LOS.
2. `NPCCombatSystem`:
исполнение melee/ranged через runtime-компоненты.
3. `NPCUtilitySystem`:
поиск и ранжирование целей через utility query.
4. `NpcFactionSystem`:
определение hostile/friendly множества.

## Практические выводы для автора поведения

1. Определи behavior в прототипах (compound/primitive/preconditions/services) до написания C#.
2. Проверяй, можно ли решить задачу существующими операторами и utility query.
3. Если нужен новый код, добавляй узкий extension point, а не переписывай pipeline.
4. Всегда проектируй явный fallback путь (idle/noop), иначе NPC будет часто без плана.
