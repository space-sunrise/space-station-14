---
name: ss14-npc-system-core
description: Глубокий разбор NPC-системы в SS14/Sunrise: HTN-планирование, utility-выбор целей, steering/pathfinding, blackboard и контракты выполнения. Используй, когда нужно понять общую схему и логику NPC, создать или переработать прототипы поведения (`htnCompound`, `rootTask`, `blackboard`), либо написать свой код ИИ (операторы, предусловия, компоненты, системы) и безопасно встроить его в runtime.
---

# NPC System: Архитектура, Прототипы и Кастомный ИИ

Используй этот skill как основной playbook по NPC в SS14/Sunrise.
Держи фокус на свежем коде и проверяй актуальность через `git log`/`git blame` (cutoff: `2024-02-20`).

## Что загружать в первую очередь

1. `references/architecture-runtime.md` — общая схема работы, слои и жизненный цикл NPC.
2. `references/behavior-prototypes.md` — как проектировать и писать прототипы поведения (`htnCompound`, `utilityQuery`, `HTN` component).
3. `references/custom-ai-code.md` — как и где писать свой код ИИ (операторы, предусловия, системы, компоненты).
4. `references/debug-validation.md` — отладка, проверка и защита от регрессий.

## Общая схема работы NPC

1. `NPCSystem` обновляет только активных NPC (`ActiveNPCComponent`) и ограничивает бюджет апдейта (`npc.max_updates`).
2. `HTNSystem` планирует поведение через time-sliced job queue (`HTNPlanJob`) и исполняет текущий план.
3. `HTNPlanJob` разворачивает дерево `HTNCompoundTask` -> `HTNPrimitiveTask`, выбирая branch по preconditions.
4. `HTNOperator.Plan()` оценивает валидность шага и может вернуть `effects` для blackboard.
5. `HTNOperator.Startup()/Update()/TaskShutdown()` выполняет runtime-логику.
6. Тяжелую/координируемую логику оператор обычно делегирует внешним системам через компоненты (steering/combat и т.п.).
7. `NPCSteeringSystem` ведет перемещение, pathfinding и obstacle avoidance.
8. `NPCUtilitySystem` выбирает цели через `utilityQuery` + considerations/curves.
9. `NpcFactionSystem` влияет на выбор враждебных/дружественных целей.

## Принципы и логика проектирования

1. Держи `Plan()` максимально чистым и предсказуемым: вычисляй, а не мутируй мир.
2. Выполняй side effects в `Startup()`/`Update()`, а cleanup в shutdown-хуках.
3. Разделяй “выбрать цель”, “дойти до цели”, “сделать действие” на разные примитивы.
4. Используй blackboard как единый контракт между прототипом и кодом.
5. Проектируй behavior как композицию маленьких compounds, а не giant-tree.
6. Всегда добавляй fallback-ветку (idle/noop), чтобы NPC не зависал без плана.
7. Для движений и боя опирайся на существующие системы (`NPCSteeringSystem`, `NPCCombatSystem`), а не на ad-hoc логику в операторе.

## Паттерны

1. Начинать root-behavior с приоритетной ветки (combat/goal), затем fallback (idle/follow).
2. Добавлять `UtilityOperator` перед действием, если цель динамическая.
3. Обновлять цель сервисом (`services`) внутри боевых примитивов.
4. Делать `MoveToOperator` отдельным шагом между выбором цели и действием.
5. Хранить диапазоны/флаги в blackboard (`VisionRadius`, `MeleeRange`, `NavInteract`) для тонкой настройки без кода.
6. Возвращать `effects` из `Plan()` для переиспользования дорогих вычислений в execution.
7. Использовать `IHtnConditionalShutdown`, когда нужен управляемый cleanup при `TaskFinished` или `PlanFinished`.
8. Писать предусловия как простые булевы gate-объекты без побочных эффектов.
9. Отключать/включать HTN через `SetHTNEnabled`/replan-паттерны вместо прямой мутации внутренних полей.
10. Валидировать дерево на рекурсивные ловушки отдельным тестом.

## Анти-паттерны

1. Писать монолитный `htnCompound`, который сложно анализировать и тестировать.
2. Делать побочные эффекты в `Plan()` (особенно сетевые/физические изменения).
3. Не чистить blackboard и runtime-компоненты при завершении задачи.
4. Полагаться только на один target key без fallback-логики.
5. Игнорировать `services`, из-за чего NPC “залипает” на устаревшей цели.
6. Смешивать pathfinding/steering/combat вручную в одном операторе.
7. Писать precondition, которая зависит от скрытого mutable-state вне blackboard/компонентов.
8. Использовать невалидные типы в `blackboard` YAML (отсутствие `!type` для сложных/явных типов).
9. Строить behavior без ветки idle/noop.
10. Игнорировать budget/cooldown (`PlanCooldown`, `npc.max_updates`) при диагностике “тупящих” NPC.

## Мини-примеры

### 1) Root prototype с fallback

```yaml
- type: htnCompound
  id: MyCustomHostileCompound
  branches:
    - tasks:
        - !type:HTNCompoundTask
          task: MeleeCombatCompound
    - tasks:
        - !type:HTNCompoundTask
          task: IdleCompound
```

### 2) Подключение HTN к сущности

```yaml
- type: entity
  id: MobMyNpc
  components:
  - type: HTN
    rootTask:
      task: MyCustomHostileCompound
    blackboard:
      VisionRadius: !type:Single
        18
      AggroVisionRadius: !type:Single
        24
      NavInteract: !type:Bool
        true
```

### 3) Кастомный оператор

```csharp
public sealed partial class MyOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        // Передать heavy/runtime-логику в отдельную систему или компонент.
        return HTNOperatorStatus.Finished;
    }
}
```

## Правило расширения

1. При добавлении новой цели/поведения сначала расширять `utilityQuery` и прототипы tasks.
2. Переходить к новому C# коду только когда прототипов и текущих операторов уже недостаточно.
3. Для новой механики сначала определить extension point (precondition/operator/system/component), затем писать код.
4. После любого расширения прогонять debug-проверку домена и smoke-тест с реальным NPC.

Думай о NPC-системе как о конвейере `планирование -> исполнение -> обслуживание состояния`, а не как о случайном наборе операторов.
