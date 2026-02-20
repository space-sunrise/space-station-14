# Rejected Snippets (VirtualController Core)

| Зона | Что найдено | Почему не брать как эталон | Сигнал |
|---|---|---|---|
| `PullController` | Комментарии про `slop`, необходимость вернуть throwing-подход, слабые места joint API | Логика рабочая, но сам файл помечает часть решений как временные/неудачные | Несколько TODO и жесткие warning-комментарии |
| `RandomWalkController` | Неясный комментарий к `prediction` параметру | Нельзя использовать как опорную документацию prediction semantics | TODO `Document this` в XML-доке |
| `SharedPhysicsSystem.Solver.GetInvMass` | Спец-ветка для `KinematicController` с пометкой о хрупкости | Внутренности полезны для понимания, но не как шаблон для нового gameplay-кода | Комментарий `shitcodey` + TODO о доработке |
| `SharedPhysicsSystem.Contacts.UpdateContact` | Временные оговорки о refactor и нестабильных сценариях удаления контактов | Не копировать как готовый архитектурный паттерн | Явные TODO/temporary-markers |
| `ConveyorController` (клиентский stub) | Пустой класс «только для prediction/networking presence» | Нельзя использовать как пример реализации conveyor-логики | Базовая дата 2023-02-13 (старше cutoff) |
| `ExitContainerOnMoveSystem` | Старый eject-through-climb flow | Исторически важен, но как эталон свежей API-практики не подходит | Дата 2024-01-14 (старше cutoff) |
| `NPC obstacle climbing flow` | Кластер TODO и workaround-логики вокруг climb/smash | Рискованно копировать как «правильный» путь интеграции с ClimbSystem | Много TODO/hack комментариев |
| `ClimbSystem` (контактный хвост stop-логики) | Участки с TODO про engine cleanup и устаревшую необходимость | Не использовать как baseline для нового контроллера завершения контакта | TODO `Remove this on engine` / `Is this needed` |
| Docs: `physics` страница | Есть TODO-маркер по событиям fixture/bodytype | Документация не закрывает все детали текущей реализации | HTML TODO marker в документе |
