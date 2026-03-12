# Fresh Pattern Catalog (VirtualController Core)

> Статус `Ограниченно` означает архитектурный контракт: полезно для понимания, но код старше cutoff и не копируется как шаблон.

| Класс/метод | Паттерн | Почему полезно | Слой | Дата по blame | Статус |
|---|---|---|---|---|---|
| `VirtualController.Initialize` | Подписка на `PhysicsUpdateBeforeSolveEvent`/`PhysicsUpdateAfterSolveEvent` через единый boilerplate | Базовый runtime-контракт всех virtual controllers | Engine | 2022-01-16 | Ограниченно |
| `VirtualController.UpdateBeforeSolve/UpdateAfterSolve` | Два симметричных хука pre/post-solver | Явная точка расширения контроллеров | Engine | 2021-03-08 | Ограниченно |
| `SharedPhysicsSystem.SimulateWorld` | Поднятие before/after событий внутри каждого substep | Фиксирует реальную частоту вызова virtual controllers | Engine | 2022-12-25 | Ограниченно |
| `SharedPhysicsSystem.SimulateWorld` | `FindNewContacts` + `Step` в substep loop | Показывает место контроллеров в contact/solver pipeline | Engine | 2025-05-28 | Использовать |
| `SharedMoverController.Initialize` | `UpdatesBefore.Add(typeof(TileFrictionController))` до `base.Initialize()` | Стабильный порядок mover vs friction | Shared | 2025-05-28 | Использовать |
| `TileFrictionController.UpdateBeforeSolve` | Ручной damping веткой для `BodyType.KinematicController` | Корректное затухание для контроллерных тел | Shared | 2025-05-02 | Использовать |
| `SharedConveyorController.UpdateBeforeSolve` | Parallel compute (`_parallel.ProcessNow`) + combine с `wishDir` | Производительный conveyor without desync | Shared | 2025-03-28 | Использовать |
| `PullController.UpdateBeforeSolve` | Импульс в pullable + обратный импульс puller в weightless/blocked | Физически стабильный pull в сложных условиях | Server | 2024-05-27 | Использовать |
| `MoverController (Client).OnUpdate*Predicted` | `UpdateIsPredictedEvent` для mover/relay target/pullable | Снижает mispredict в локальном управлении | Client | 2024-09-12 | Использовать |
| `SharedMoverController.SetRelay` | Relay lifecycle с `PhysicsSystem.UpdateIsPredicted(...)` | Консистентная relay-синхронизация | Shared | 2025-04-05 | Использовать |
| `SharedMoverController.RemoveRelay` | Явный teardown relay + cleanup prediction state | Избегает подвисших relay-целей | Shared | 2025-08-04 | Использовать |
| `ChasingWalkSystem` | Установка скорости + `SetBodyStatus(..., BodyStatus.InAir)` для спец-сущностей | Поддерживает нужную механику преследования | Server | 2024-03-25 | Использовать |
| `ChaoticJumpSystem.Jump` | Raycast-выбор цели и safe-offset телепорта перед `SetWorldPosition` | Снижает шанс телепорта в коллизию | Server | 2024-09-29 | Использовать |
