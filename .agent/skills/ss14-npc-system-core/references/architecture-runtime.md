# Architecture and Runtime Map

## Purpose

Use this file when you need to quickly remember in your head exactly how NPC runtime works in SS14/Sunrise.

## Layer map

1. NPC orchestration:
`Content.Server/NPC/Systems/NPCSystem.cs`
2. HTN runtime:
`Content.Server/NPC/HTN/HTNSystem.cs`
3. Scheduler:
`Content.Server/NPC/HTN/HTNPlanJob.cs`
4. Plan details:
`Content.Server/NPC/HTN/HTNPlan.cs`
5. Basic contracts tasks:
`Content.Server/NPC/HTN/HTNTask.cs`
`Content.Server/NPC/HTN/HTNCompoundTask.cs`
`Content.Server/NPC/HTN/PrimitiveTasks/HTNPrimitiveTask.cs`
6. Expansion contracts:
`Content.Server/NPC/HTN/PrimitiveTasks/HTNOperator.cs`
`Content.Server/NPC/HTN/Preconditions/HTNPrecondition.cs`
`Content.Server/NPC/HTN/IHtnConditionalShutdown.cs`
7. Blackboard:
`Content.Server/NPC/NPCBlackboard.cs`
`Content.Server/NPC/NPCBlackboardSerializer.cs`
8. Navigation and steering:
`Content.Server/NPC/Systems/NPCSteeringSystem.cs`
9. Utility-assessment of goals:
`Content.Server/NPC/Systems/NPCUtilitySystem.cs`
`Content.Server/NPC/Queries/UtilityQueryPrototype.cs`
10. Factions:
`Content.Shared/NPC/Systems/NpcFactionSystem.cs`

## NPC life cycle (runtime)

1. At `MapInit` the NPC “wakes up” (`WakeNPC`) and receives `ActiveNPCComponent`.
2. `NPCSystem.Update()` calls `HTNSystem.UpdateNPC(...)` within budget limits.
3. `HTNSystem`:
processes the planning queue -> picks up a ready plan job -> if necessary, replaces the current plan -> executes `Update()` of the current operator.
4. If the statement has completed:
shutdown is executed, the plan index is shifted, and the next task is launched.
5. If the operator falls:
the current plan is extinguished and a new planning cycle is initiated.
6. If an entity is dead/turned off/occupied by a player:
The NPC is put into sleep, the active components of the behavior are removed.

## Planning and execution: strict boundary

1. Planning:
runs asynchronously (job queue), checks preconditions and `Plan()`.
2. Execution:
executed in a normal world update via `Startup()`/`Update()`/shutdown.
3. Effects:
The data returned from `Plan()` can be applied to the startup step (reuse the planning results).

## HTN decomposition: how a branch is selected

1. For `HTNCompoundTask` branches are sorted from top to bottom.
2. The first branch where all branch-preconditions are true goes onto the decomposition stack.
3. If the plan then falls apart at a child step, the planner rolls back the state (blackboard + selected primitives) and tries the next branch.
4. If all branches are failed, the current compound point is considered unsolvable.

## Blackboard model

1. Blackboard stores NPC status and configurable parameters.
2. The keys are strings, there are basic constants (`Owner`, `Target`, `MovementPathfind`, `NavInteract`, etc.).
3. There are defaults (for example, `VisionRadius`, `MeleeRange`, `FollowRange`) that are picked up even without an explicit entry.
4. During planning, the board can operate in read-only mode.
5. For YAML blackboard, typed serialization is used (`!type:Single`, `!type:Bool`, `!type:SoundPathSpecifier`, etc.).

## Service subsystems that are typically involved in behavior

1. `NPCSteeringSystem`:
movement task registration, pathfinding, obstacle avoidance, completion by range/LOS.
2. `NPCCombatSystem`:
execution of melee/ranged via runtime components.
3. `NPCUtilitySystem`:
search and ranking of targets via utility query.
4. `NpcFactionSystem`:
definition of hostile/friendly set.

## Practical conclusions for the author of behavior

1. Define behavior in prototypes (compound/primitive/preconditions/services) before writing C#.
2. Check whether the problem can be solved using existing operators and utility queries.
3. If you need new code, add a narrow extension point, rather than rewrite the pipeline.
4. Always design an explicit fallback path (idle/noop), otherwise the NPC will often be without a plan.
