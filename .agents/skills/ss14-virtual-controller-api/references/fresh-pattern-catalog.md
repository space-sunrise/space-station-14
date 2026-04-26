# Fresh Pattern Catalog (VirtualController API)

> The status `Limited` means: the method/pattern is important for the API contract, but the line is older than the cutoff and should not be copied without additional verification.

| Class/method | Pattern | Why is it useful | Layer | Date by blame | Status |
|---|---|---|---|---|---|
| `VirtualController.UpdateBeforeSolve` | Pre-solver controller entry point | Basic API contract for virtual controllers | Engine | 2021-03-08 | Limited |
| `VirtualController.UpdateAfterSolve` | Post-solver entry point | Required for correct post-step behavior | Engine | 2021-03-08 | Limited |
| `UpdatesBefore/UpdatesAfter` (using mover as an example) | Order is configured to `base.Initialize()` | Controlled sequence of controllers | Shared | 2025-05-28 | Use |
| `SharedPhysicsSystem.Components.SetLinearVelocity` | System speed mutator | Safer than direct field changes | Engine | 2024-04-23 | Use |
| `SharedPhysicsSystem.Components.ApplyLinearImpulse` | Impulse via resolve+wake-check path | Standard for pull/force scripts | Engine | 2024-04-29 | Use |
| `SharedPhysicsSystem.Components.SetBodyStatus` | Explicit change `BodyStatus` | Needed for narrow movement mechanics | Engine | 2024-03-18 | Use |
| `SharedPhysicsSystem.Components.SetLinearDamping` | Runtime adjustment of linear attenuation | Slip/damping control | Engine | 2024-03-18 | Use |
| `SharedPhysicsSystem.Components.SetAngularDamping` | Runtime adjustment of corner attenuation | Stabilizes rotation | Engine | 2024-03-18 | Use |
| `SharedMoverController.SetRelay` | Installing relay + prediction sync | Consistent relay lifecycle | Shared | 2025-04-05 | Use |
| `SharedMoverController.RemoveRelay` | Explicit teardown of relay bundles | Does not leave hanging relay-states | Shared | 2025-08-04 | Use |
| `SharedMoverController.GetWishDir/SetWishDir` | Working with the desired motion vector | Base point for mover/conveyor integration | Shared | 2025-03-28 | Use |
| `SharedMoverController.Friction` | Built-in speed decay API | Unified mathematics of motion | Shared | 2025-03-28 | Use |
| `SharedMoverController.Accelerate` | Limited acceleration to target vector | Predictable acceleration without jumps | Shared | 2025-03-28 | Use |
| `SharedMoverController.ResetCamera` | Reset Relative Camera Angle | Working helper, but old API layer | Shared | 2022-08-29 | Limited |
| `SharedMoverController.GetParentGridAngle` | Calculating parent/grid angle | Basic helper for relative orientation | Shared | 2023-08-01 | Limited |
| `TileFrictionController.SetModifier` | Runtime change of tile-friction modifier | Useful, but the original method is old | Shared | 2023-05-14 | Limited |
| `SharedPuddleSystem` + `SetModifier` | Practical use of the friction-modifier API | Current gameplay case of runtime friction | Gameplay | 2025-10-18 | Use |
| `ClimbSystem.CanVault` | Prevalidation with guard logic (including container-case) | Reduces false starts climb-flow | Shared | 2024-08-11 | Use |
| `ClimbSystem.TryClimb` | DoAfter-based start + state consistency | Safe start of the climb process | Shared | 2025-04-14 | Use |
| `ClimbSystem.ForciblySetClimbing` | Forced transfer to climbing-state | The required API, but the implementation is older than cutoff | Shared | 2024-01-01 | Limited |
| `SharedCryoPodSystem` + `ForciblySetClimbing` | Post-eject entity stabilization via climb API | Fresh production case container-eject | Gameplay | 2025-08-06 | Use |
| `SharedConveyorController.UpdateBeforeSolve` | Compute/apply conveyor flow + wake | Stable conveyor API practice | Shared | 2025-03-28 | Use |
| `PullController.UpdateBeforeSolve` | Mass-based impulse + inverse-impulse branch | Secure pull API in challenging environments | Server | 2024-05-27 | Use |
| `MoverController (Client).OnUpdatePredicted` | Prediction hook for local mover | Client-side control prediction database | Client | 2024-09-12 | Use |
| `MoverController (Client).OnUpdateRelayTargetPredicted` | Prediction hook for relay target | Needed for proxy-control | Client | 2024-09-12 | Use |
| `MoverController (Client).OnUpdatePullablePredicted` | Prediction hook for pullable | Prevents false local prediction | Client | 2024-09-12 | Use |
| `SharedStationAiSystem` relay-piloting | Relay case station-eye control | Fresh non-standard proxy-control script | Gameplay | 2024-08-28 | Use |
| `PilotedClothingSystem` relay-piloting | Relay via wearable object | Practice control-transfer between entities | Gameplay | 2024-06-18 | Use |
| `VentCrawTubeSystem` relay-piloting | Relay proxy for ventcrawl-like movement | Unique fresh proxy-control case | Gameplay | 2025-02-10 | Use |
| `SharedMechSystem` relay-piloting | Relay on fur | Architecturally important case, but the implementation is old | Gameplay | 2023-05-13 | Limited |
| `CardboardBoxSystem` relay control | Relay in storage scenario | Useful case, but older cutoff | Gameplay | 2023-12-17 | Limited |
| `DragInsertContainerSystem` + `ForciblySetClimbing` | Empty/eject container with climb-post-step | Historically useful, but pre-cutoff | Gameplay | 2024-01-15 | Limited |
