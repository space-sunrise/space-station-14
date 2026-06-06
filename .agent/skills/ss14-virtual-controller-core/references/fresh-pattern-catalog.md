# Fresh Pattern Catalog (VirtualController Core)

> Status `Limited` means architectural contract: useful for understanding, but the code is older than cutoff and is not copied as a template.

| Class/method | Pattern | Why is it useful | Layer | Date by blame | Status |
|---|---|---|---|---|---|
| `VirtualController.Initialize` | Subscription to `PhysicsUpdateBeforeSolveEvent`/`PhysicsUpdateAfterSolveEvent` through a single boilerplate | Basic runtime contract for all virtual controllers | Engine | 2022-01-16 | Limited |
| `VirtualController.UpdateBeforeSolve/UpdateAfterSolve` | Two symmetrical pre/post-solver hooks | Explicit controller extension point | Engine | 2021-03-08 | Limited |
| `SharedPhysicsSystem.SimulateWorld` | Raising before/after events within each substep | Fixes the actual call frequency of virtual controllers | Engine | 2022-12-25 | Limited |
| `SharedPhysicsSystem.SimulateWorld` | `FindNewContacts` + `Step` in substep loop | Shows the place of controllers in the contact/solver pipeline | Engine | 2025-05-28 | Use |
| `SharedMoverController.Initialize` | `UpdatesBefore.Add(typeof(TileFrictionController))` to `base.Initialize()` | Stable order mover vs friction | Shared | 2025-05-28 | Use |
| `TileFrictionController.UpdateBeforeSolve` | Manual damping branch for `BodyType.KinematicController` | Correct attenuation for controller bodies | Shared | 2025-05-02 | Use |
| `SharedConveyorController.UpdateBeforeSolve` | Parallel compute (`_parallel.ProcessNow`) + combine with `wishDir` | Productive conveyor without desync | Shared | 2025-03-28 | Use |
| `PullController.UpdateBeforeSolve` | Pulse in pullable + reverse pulse puller in weightless/blocked | Physically stable pull in difficult conditions | Server | 2024-05-27 | Use |
| `MoverController (Client).OnUpdate*Predicted` | `UpdateIsPredictedEvent` for mover/relay target/pullable | Reduces mispredict in local management | Client | 2024-09-12 | Use |
| `SharedMoverController.SetRelay` | Relay lifecycle with `PhysicsSystem.UpdateIsPredicted(...)` | Consistent relay synchronization | Shared | 2025-04-05 | Use |
| `SharedMoverController.RemoveRelay` | Explicit teardown relay + cleanup prediction state | Avoids dangling relay targets | Shared | 2025-08-04 | Use |
| `ChasingWalkSystem` | Setting speed + `SetBodyStatus(..., BodyStatus.InAir)` for special entities | Supports desired pursuit mechanics | Server | 2024-03-25 | Use |
| `ChaoticJumpSystem.Jump` | Raycast-target selection and teleport safe-offset before `SetWorldPosition` | Reduces the chance of teleporting into a collision | Server | 2024-09-29 | Use |
