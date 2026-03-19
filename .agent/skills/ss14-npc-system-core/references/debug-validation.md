# Debug and Validation Guide

## Назначение

Используй этот файл, когда нужно быстро проверить, почему NPC ведет себя не так, как ожидалось.

## Базовая отладка поведения

1. Посмотреть домен HTN root/compound:
`npcdomain <compoundId>`
2. Открыть debug UI по NPC:
`npc`
3. Включить client overlay HTN:
`showhtn`
4. Временно навесить HTN на сущность:
`addnpc <entityUid> <rootTask>`

## Что проверять в первую очередь

1. У сущности есть `HTN` компонент и правильный `rootTask`.
2. У сущности есть `ActiveNPCComponent` (NPC не “спит”).
3. Ключи blackboard существуют и имеют верные типы/значения.
4. Ветка с ожидаемым действием действительно проходит preconditions.
5. Оператор возвращает адекватный статус (`Continuing/Finished/Failed`).

## Диагностика “NPC не двигается”

1. Проверить наличие `NPCSteeringComponent` во время выполнения movement-задачи.
2. Проверить pathfinding-флаги и целевые координаты (`TargetCoordinates`, `MovementPathfind`).
3. Проверить range/LOS условия (`stopOnLineOfSight`, `rangeKey`, preconditions).
4. Проверить `npc.pathfinding` и `npc.enabled`.

## Диагностика “NPC не атакует”

1. Проверить целевой key (`Target`) после `UtilityOperator`.
2. Проверить сервисы в боевом примитиве (обновление target).
3. Проверить боевой runtime-компонент (`NPCMeleeCombatComponent`/`NPCRangedCombatComponent`).
4. Проверить preconditions по дистанции/LOS/состоянию цели.
5. Проверить фракции (`NpcFactionMember` + relations).

## Диагностика “план странный/дергается”

1. Проверить слишком частый replan (`PlanCooldown`, `ConstantlyReplan`).
2. Проверить ветки на конфликтующие preconditions.
3. Проверить giant-compound с перекрывающимися ветками приоритета.
4. Проверить, что operator cleanup корректно снимает runtime-state.

## Валидация изменений

1. Запустить точечный smoke-тест:
заспавнить NPC, проверить выбор веток и выполнение ключевого сценария.
2. Прогнать проверку на рекурсивные ловушки в compounds (интеграционный тест NPC recursion).
3. Прогнать локальные тесты/линтеры, связанные с NPC и прототипами.
4. Проверить, что изменения не ломают существующие root compounds.

## Мини-чеклист перед PR

1. Новый behavior имеет fallback-ветку.
2. Новый оператор/предусловие имеет ясный shutdown-контракт.
3. Blackboard-ключи документированы и не конфликтуют по типам.
4. Для нового utility-профиля есть обоснованный набор considerations.
5. Есть минимальная проверка в игре или тесте, подтверждающая ожидаемое поведение.
