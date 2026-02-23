# Rejected Snippets (VirtualController API)

| Зона | Что найдено | Почему не брать как эталон | Сигнал |
|---|---|---|---|
| `PullController` | Внутренние комментарии о "slop" и необходимости другого подхода | Реализация функционирует, но содержит признанный технический долг | TODO + warning-комментарии |
| `RandomWalkController` | Неясность по semantics `prediction` в документации метода | Нельзя использовать как reference по контракту prediction | TODO `Document this` |
| `SharedPhysicsSystem.Solver.GetInvMass` | Специальная ветка `KinematicController` с негативным комментарием | Важный внутренний контекст, но плохой API-шаблон для gameplay | Комментарий о хрупкости + TODO |
| `SharedPhysicsSystem.Contacts.UpdateContact` | Временные оговорки по контактам и удалению | Не копировать как «чистый API-подход» | `temporary`/TODO markers |
| Прямой `RemComp<RelayInputMoverComponent>` в gameplay | Обход `RemoveRelay` lifecycle-метода | Риск оставлять несогласованные relay-target состояния | Manual component surgery |
| Пустой клиентский `ConveyorController` | Только network/prediction presence без бизнес-логики | Нельзя брать как API-референс conveyor поведения | Базовая дата 2023-02-13 (старше cutoff) |
| `ExitContainerOnMoveSystem` | Старый container-exit flow через climb | Полезен исторически, но не как свежий эталон | Дата 2024-01-14 (старше cutoff) |
| NPC obstacle climbing блок | Набор TODO и workaround-условий вокруг препятствий | Низкая переносимость, высокий риск регрессий | TODO-heavy комментарии |
| Чрезмерное использование `BodyStatus.InAir` | Часто выглядит как quick-fix вне спец-механики | Может маскировать реальную проблему в физике/контактах | Semantic misuse |
| Docs `physics` page | Устаревший контент и неполный coverage по событиям | Нельзя полагаться как на актуальное API-руководство | TODO marker + старая дата |
