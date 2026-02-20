# Behavior Prototypes Guide

## Назначение

Используй этот файл, когда нужно собрать или переработать behavior NPC на уровне YAML без лишнего C#.

## Содержание

1. Где лежат прототипы поведения
2. Скелет behavior: root compound
3. Скелет compound с предусловиями и примитивами
4. Подключение к entity prototype
5. Рецепт проектирования поведения (от идеи к YAML)
6. Когда делать новый utilityQuery
7. Паттерны устойчивых прототипов
8. Анти-паттерны прототипов
9. Мини-чеклист перед коммитом

## Где лежат прототипы поведения

1. Базовые HTN compounds:
`Resources/Prototypes/NPCs/*.yml`
2. Бой:
`Resources/Prototypes/NPCs/Combat/*.yml`
3. Utility-запросы:
`Resources/Prototypes/NPCs/utility_queries.yml`
4. Подключение поведения к сущностям:
`Resources/Prototypes/Entities/Mobs/NPCs/*.yml`
и форковые пакеты в `Resources/Prototypes/_Sunrise/**`

## Скелет behavior: root compound

```yaml
- type: htnCompound
  id: MyNpcRootCompound
  branches:
    - tasks:
        - !type:HTNCompoundTask
          task: MyPrimaryGoalCompound
    - tasks:
        - !type:HTNCompoundTask
          task: IdleCompound
```

Правило:
ставь более “ценные” ветки выше, fallback-ниже.

## Скелет compound с предусловиями и примитивами

```yaml
- type: htnCompound
  id: MyPrimaryGoalCompound
  branches:
    - preconditions:
        - !type:KeyExistsPrecondition
          key: Target
      tasks:
        - !type:HTNPrimitiveTask
          operator: !type:MoveToOperator
            targetKey: TargetCoordinates
            rangeKey: MeleeRange

        - !type:HTNPrimitiveTask
          preconditions:
            - !type:TargetInRangePrecondition
              targetKey: Target
              rangeKey: MeleeRange
          operator: !type:MeleeOperator
            targetKey: Target
          services:
            - !type:UtilityService
              id: Retarget
              proto: NearbyMeleeTargets
              key: Target
```

## Подключение к entity prototype

```yaml
- type: entity
  id: MobMyNpc
  components:
  - type: HTN
    rootTask:
      task: MyNpcRootCompound
    blackboard:
      VisionRadius: !type:Single
        16
      AggroVisionRadius: !type:Single
        24
      MeleeRange: !type:Single
        1.2
      NavInteract: !type:Bool
        true
      NavPry: !type:Bool
        false
```

## Рецепт проектирования поведения (от идеи к YAML)

1. Сформулируй цель NPC в терминах “выбрать цель -> подойти -> выполнить действие”.
2. Выдели root compound и 2-4 приоритетные ветки.
3. Вынеси повторяемые куски в отдельные compounds (например `BeforeAttack`, `PickupWeapon`, `Follow`).
4. Для динамических целей добавь `UtilityOperator` + `UtilityService`.
5. Используй preconditions как gate-слой, а не как место для сложной логики.
6. Добавь fallback-ветку (`IdleCompound`/`NoOperator`) на случай провала остальных.
7. Подключи rootTask в `HTN` компоненте сущности.
8. Настрой blackboard-ключи под конкретный archetype NPC.

## Когда делать новый utilityQuery

Создавай новый `utilityQuery`, если одновременно нужны:
1. новый источник кандидатов (набор query/filter);
2. новая метрика выбора (consideration/curve);
3. отдельный reusable профиль ранжирования для нескольких NPC.

## Паттерны устойчивых прототипов

1. Разделяй compound “по обязанностям”, а не по типам мобов.
2. Держи ветки короткими (обычно 2-5 задач в branch).
3. Используй services в long-running задачах, где target может устареть.
4. Используй blackboard range-ключи вместо магических чисел в операторах.
5. В бою добавляй `MoveToOperator` перед attack, даже если кажется “и так рядом”.

## Анти-паттерны прототипов

1. Дублировать один и тот же кусок branch в десятках compounds вместо переиспользования.
2. Пытаться решить все через preconditions без отдельного оператора.
3. Не указывать fallback и оставлять behavior “без выхода”.
4. Ставить широкие ветки выше узких и получать случайный перехват плана.
5. Писать root compound с циклическими ссылками без контроля recursion.

## Мини-чеклист перед коммитом

1. Все `task:` ID существуют среди `htnCompound`.
2. Для всех новых `!type:...` есть соответствующий C# data definition.
3. `HTN` компонент сущности получает корректный `rootTask`.
4. Blackboard-ключи используют корректные типы (`!type:Single`, `!type:Bool`, ...).
5. Есть fallback-ветка.
