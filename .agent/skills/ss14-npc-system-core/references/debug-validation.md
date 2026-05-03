# Debug and Validation Guide

## Purpose

Use this file when you need to quickly check why an NPC is not behaving as expected.

## Basic behavior debugging

1. View the HTN root/compound domain:
`npcdomain <compoundId>`
2. Open debug UI by NPC:
`npc`
3. Enable client overlay HTN:
`showhtn`
4. Temporarily attach HTN to the entity:
`addnpc <entityUid> <rootTask>`

## What to check first

1. The entity has a `HTN` component and a valid `rootTask`.
2. The entity has `ActiveNPCComponent` (NPC is not “sleeping”).
3. Blackboard keys exist and have the correct types/values.
4. The branch with the expected action actually goes through preconditions.
5. The operator returns an adequate status (`Continuing/Finished/Failed`).

## Diagnostics “NPC does not move”

1. Check for the presence of `NPCSteeringComponent` during the execution of the movement task.
2. Check pathfinding flags and target coordinates (`TargetCoordinates`, `MovementPathfind`).
3. Check range/LOS conditions (`stopOnLineOfSight`, `rangeKey`, preconditions).
4. Check `npc.pathfinding` and `npc.enabled`.

## Diagnostics “NPC does not attack”

1. Check the target key (`Target`) after `UtilityOperator`.
2. Check the services in the combat primitive (target update).
3. Check the combat runtime component (`NPCMeleeCombatComponent`/`NPCRangedCombatComponent`).
4. Check preconditions for distance/LOS/target condition.
5. Check factions (`NpcFactionMember` + relations).

## Diagnostics “the plan is strange/twitching”

1. Check for too frequent replan (`PlanCooldown`, `ConstantlyReplan`).
2. Check branches for conflicting preconditions.
3. Check giant-compound with overlapping priority branches.
4. Check that operator cleanup correctly removes the runtime-state.

## Validation of changes

1. Run a spot smoke test:
spawn the NPC, check the choice of branches and the execution of the key script.
2. Run a test for recursive traps in compounds (NPC recursion integration test).
3. Run local tests/linters related to NPCs and prototypes.
4. Check that the changes do not break existing root compounds.

## Mini checklist before PR

1. The new behavior has a fallback branch.
2. The new operator/precondition has a clear shutdown contract.
3. Blackboard keys are documented and do not conflict by type.
4. There is a reasonable set of considerations for the new utility profile.
5. There is a minimum check in the game or test that confirms the expected behavior.
