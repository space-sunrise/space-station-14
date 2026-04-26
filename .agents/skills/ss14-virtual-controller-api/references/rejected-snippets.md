# Rejected Snippets (VirtualController API)

| Zone | What's found | Why not take it as a standard | Signal |
|---|---|---|---|
| `PullController` | Internal comments about "slop" and the need for a different approach | Implementation works, but contains recognized technical debt | TODO + warning comments |
| `RandomWalkController` | Ambiguity on semantics `prediction` in the method documentation | Cannot be used as a reference under the prediction contract | TODO `Document this` |
| `SharedPhysicsSystem.Solver.GetInvMass` | Special thread `KinematicController` with negative comment | Important internal context, but poor API pattern for gameplay | Comment on fragility + TODO |
| `SharedPhysicsSystem.Contacts.UpdateContact` | Temporary contact and deletion clauses | Don't copy as a "pure API approach" | `temporary`/TODO markers |
| Direct `RemComp<RelayInputMoverComponent>` in gameplay | Bypass `RemoveRelay` lifecycle method | Risk of leaving inconsistent relay-target states | Manual component surgery |
| Empty client `ConveyorController` | Only network/prediction presence without business logic | Cannot be used as an API reference for conveyor behavior | Base date 2023-02-13 (older than cutoff) |
| `ExitContainerOnMoveSystem` | Old container-exit flow via climb | Useful historically, but not as a fresh reference | Date 2024-01-14 (older than cutoff) |
| NPC obstacle climbing block | Set of TODO and workaround conditions around obstacles | Low tolerance, high risk of regressions | TODO-heavy comments |
| Excessive use of `BodyStatus.InAir` | Often looks like a quick-fix outside of special mechanics | May mask a real problem in physics/contacts | Semantic misuse |
| Docs `physics` page | Outdated content and incomplete event coverage | The current API manual should not be relied upon | TODO marker + old date |
