# Rejected Snippets (VirtualController Core)

| Zone | What's found | Why not take it as a standard | Signal |
|---|---|---|---|
| `PullController` | Comments about `slop`, the need to return the throwing approach, weaknesses of the joint API | The logic is working, but the file itself marks some of the solutions as temporary/failed | Several TODOs and strict warning comments |
| `RandomWalkController` | Unclear comment for `prediction` parameter | Cannot be used as reference documentation prediction semantics | TODO `Document this` in XML doc |
| `SharedPhysicsSystem.Solver.GetInvMass` | Special branch for `KinematicController` with a note about fragility | The internals are useful for understanding, but not as a template for new gameplay code | Comment `shitcodey` + TODO about improvement |
| `SharedPhysicsSystem.Contacts.UpdateContact` | Temporary caveats about refactor and unstable contact deletion scenarios | Do not copy as a ready-made architectural pattern | Explicit TODO/temporary-markers |
| `ConveyorController` (client stub) | Empty class “prediction/networking presence only” | Cannot be used as an example of conveyor logic implementation | Base date 2023-02-13 (older than cutoff) |
| `ExitContainerOnMoveSystem` | Old eject-through-climb flow | Historically important, but not suitable as a standard for fresh API practice | Date 2024-01-14 (older than cutoff) |
| `NPC obstacle climbing flow` | TODO cluster and workaround logic around climb/smash | It's risky to copy as the "correct" way to integrate with ClimbSystem | Lots of TODO/hack comments |
| `ClimbSystem` (pin tail of stop logic) | Sites with TODO about engine cleanup and obsolete necessity | Do not use as baseline for new contact completion controller | TODO `Remove this on engine` / `Is this needed` |
| Docs: `physics` page | There is a TODO marker for fixture/bodytype events | The documentation does not cover all the details of the current implementation | HTML TODO marker in document |
