# Fresh Pattern Catalog (VirtualController API)

> Статус `Ограниченно` означает: метод/паттерн важен для API-контракта, но строка старше cutoff и не должна копироваться без дополнительной сверки.

| Класс/метод | Паттерн | Почему полезно | Слой | Дата по blame | Статус |
|---|---|---|---|---|---|
| `VirtualController.UpdateBeforeSolve` | Pre-solver точка входа контроллера | Базовый API-контракт virtual controllers | Engine | 2021-03-08 | Ограниченно |
| `VirtualController.UpdateAfterSolve` | Post-solver точка входа | Нужен для корректного post-step поведения | Engine | 2021-03-08 | Ограниченно |
| `UpdatesBefore/UpdatesAfter` (на примере mover) | Order настраивается до `base.Initialize()` | Управляемая последовательность контроллеров | Shared | 2025-05-28 | Использовать |
| `SharedPhysicsSystem.Components.SetLinearVelocity` | Системный mutator скорости | Безопаснее прямого изменения поля | Engine | 2024-04-23 | Использовать |
| `SharedPhysicsSystem.Components.ApplyLinearImpulse` | Impulse через resolve+wake-check path | Стандарт для pull/force сценариев | Engine | 2024-04-29 | Использовать |
| `SharedPhysicsSystem.Components.SetBodyStatus` | Явная смена `BodyStatus` | Нужно для узких movement-механик | Engine | 2024-03-18 | Использовать |
| `SharedPhysicsSystem.Components.SetLinearDamping` | Runtime настройка линейного затухания | Контроль скольжения/гашения | Engine | 2024-03-18 | Использовать |
| `SharedPhysicsSystem.Components.SetAngularDamping` | Runtime настройка углового затухания | Стабилизирует вращение | Engine | 2024-03-18 | Использовать |
| `SharedMoverController.SetRelay` | Установка relay + prediction sync | Консистентный relay lifecycle | Shared | 2025-04-05 | Использовать |
| `SharedMoverController.RemoveRelay` | Явный teardown relay-связки | Не оставляет подвисшие relay-state | Shared | 2025-08-04 | Использовать |
| `SharedMoverController.GetWishDir/SetWishDir` | Работа с желаемым вектором движения | Базовая точка для mover/conveyor интеграции | Shared | 2025-03-28 | Использовать |
| `SharedMoverController.Friction` | Встроенный API затухания скорости | Единая математика движения | Shared | 2025-03-28 | Использовать |
| `SharedMoverController.Accelerate` | Ограниченное ускорение к целевому вектору | Предсказуемый разгон без скачков | Shared | 2025-03-28 | Использовать |
| `SharedMoverController.ResetCamera` | Сброс относительного угла камеры | Рабочий helper, но старый API-слой | Shared | 2022-08-29 | Ограниченно |
| `SharedMoverController.GetParentGridAngle` | Вычисление parent/grid угла | Базовый helper для relative-ориентации | Shared | 2023-08-01 | Ограниченно |
| `TileFrictionController.SetModifier` | Runtime изменение tile-friction модификатора | Полезно, но исходный метод старый | Shared | 2023-05-14 | Ограниченно |
| `SharedPuddleSystem` + `SetModifier` | Практическое использование friction-modifier API | Актуальный gameplay-кейс runtime friction | Gameplay | 2025-10-18 | Использовать |
| `ClimbSystem.CanVault` | Предвалидация с guard-логикой (включая container-case) | Снижает ложные старты climb-flow | Shared | 2024-08-11 | Использовать |
| `ClimbSystem.TryClimb` | DoAfter-based start + консистентность состояния | Безопасный запуск climb процесса | Shared | 2025-04-14 | Использовать |
| `ClimbSystem.ForciblySetClimbing` | Принудительный перевод в climbing-state | Нужный API, но реализация старше cutoff | Shared | 2024-01-01 | Ограниченно |
| `SharedCryoPodSystem` + `ForciblySetClimbing` | Post-eject стабилизация сущности через climb API | Свежий production-кейс container-eject | Gameplay | 2025-08-06 | Использовать |
| `SharedConveyorController.UpdateBeforeSolve` | Compute/apply conveyor flow + wake | Стабильная conveyor API-практика | Shared | 2025-03-28 | Использовать |
| `PullController.UpdateBeforeSolve` | Mass-based impulse + inverse-impulse ветка | Безопасный pull API в сложных условиях | Server | 2024-05-27 | Использовать |
| `MoverController (Client).OnUpdatePredicted` | Prediction hook для локального mover | База client-side control prediction | Client | 2024-09-12 | Использовать |
| `MoverController (Client).OnUpdateRelayTargetPredicted` | Prediction hook для relay target | Нужен для proxy-control | Client | 2024-09-12 | Использовать |
| `MoverController (Client).OnUpdatePullablePredicted` | Prediction hook для pullable | Предотвращает ложный local prediction | Client | 2024-09-12 | Использовать |
| `SharedStationAiSystem` relay-пилотирование | Relay-кейс station-eye управления | Свежий нестандартный proxy-control сценарий | Gameplay | 2024-08-28 | Использовать |
| `PilotedClothingSystem` relay-пилотирование | Relay через носимый объект | Практика control-transfer между сущностями | Gameplay | 2024-06-18 | Использовать |
| `VentCrawTubeSystem` relay-пилотирование | Relay-прокси для ventcrawl-like перемещения | Уникальный свежий кейс proxy-control | Gameplay | 2025-02-10 | Использовать |
| `SharedMechSystem` relay-пилотирование | Relay на мехе | Архитектурно важный кейс, но реализация старая | Gameplay | 2023-05-13 | Ограниченно |
| `CardboardBoxSystem` relay-контроль | Relay в storage-сценарии | Полезный кейс, но старше cutoff | Gameplay | 2023-12-17 | Ограниченно |
| `DragInsertContainerSystem` + `ForciblySetClimbing` | Empty/eject контейнера с climb-post-step | Исторически полезно, но pre-cutoff | Gameplay | 2024-01-15 | Ограниченно |
