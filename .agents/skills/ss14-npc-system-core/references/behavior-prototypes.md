# Behavior Prototypes Guide

## Purpose

Use this file when you need to assemble or rework NPC behavior at the YAML level without unnecessary C#.

## Content

1. Where do behavioral prototypes lie?
2. Skeleton behavior: root compound
3. Skeleton compound with preconditions and primitives
4. Connecting to entity prototype
5. Behavior Design Recipe (from Idea to YAML)
6. When to make a new utilityQuery
7. Stable Prototyping Patterns
8. Anti-patterns of prototypes
9. Mini-checklist before commit

## Where are the behavioral prototypes located?

1. Basic HTN compounds:
`Resources/Prototypes/NPCs/*.yml`
2. Fight:
`Resources/Prototypes/NPCs/Combat/*.yml`
3. Utility requests:
`Resources/Prototypes/NPCs/utility_queries.yml`
4. Connecting behavior to entities:
`Resources/Prototypes/Entities/Mobs/NPCs/*.yml`
and forked packages in `Resources/Prototypes/_Sunrise/**`

## Skeleton behavior: root compound

```yaml
- type: htnCompound
  id: MyNpcRootCompound
  branches:
    - tasks:
        - !type:HTNCompoundTask
          task: MyPrimaryGoalCompound
    - tasks:
        - !type:HTNCompoundTask
          task: IdleCompound
```

Rule:
put more “valuable” branches higher, fallback ones lower.

## Skeleton compound with preconditions and primitives

```yaml
- type: htnCompound
  id: MyPrimaryGoalCompound
  branches:
    - preconditions:
        - !type:KeyExistsPrecondition
          key: Target
      tasks:
        - !type:HTNPrimitiveTask
          operator: !type:MoveToOperator
            targetKey: TargetCoordinates
            rangeKey: MeleeRange

        - !type:HTNPrimitiveTask
          preconditions:
            - !type:TargetInRangePrecondition
              targetKey: Target
              rangeKey: MeleeRange
          operator: !type:MeleeOperator
            targetKey: Target
          services:
            - !type:UtilityService
              id: Retarget
              proto: NearbyMeleeTargets
              key: Target
```

## Connecting to entity prototype

```yaml
- type: entity
  id: MobMyNpc
  components:
  - type: HTN
    rootTask:
      task: MyNpcRootCompound
    blackboard:
      VisionRadius: !type:Single
        16
      AggroVisionRadius: !type:Single
        24
      MeleeRange: !type:Single
        1.2
      NavInteract: !type:Bool
        true
      NavPry: !type:Bool
        false
```

## Behavior Design Recipe (from Idea to YAML)

1. Formulate the NPC’s goal in terms of “select a target -> approach -> perform an action.”
2. Select a root compound and 2-4 priority branches.
3. Move the repeated pieces into separate compounds (for example `BeforeAttack`, `PickupWeapon`, `Follow`).
4. For dynamic purposes, add `UtilityOperator` + `UtilityService`.
5. Use preconditions as a gate layer, not as a place for complex logic.
6. Add a fallback branch (`IdleCompound`/`NoOperator`) in case the others fail.
7. Connect rootTask in the `HTN` entity component.
8. Set up blackboard keys for a specific archetype NPC.

## When to make a new utilityQuery

Create a new `utilityQuery` if you simultaneously need:
1. new source of candidates (query/filter set);
2. new selection metric (consideration/curve);
3. Separate reusable ranking profile for multiple NPCs.

## Stable Prototyping Patterns

1. Divide the compound “by responsibilities”, not by types of mobs.
2. Keep branches short (usually 2-5 issues per branch).
3. Use services in long-running tasks where the target may become outdated.
4. Use blackboard range keys instead of magic numbers in operators.
5. In battle, add `MoveToOperator` before the attack, even if it seems “already close.”

## Anti-prototype patterns

1. Duplicate the same piece of branch in dozens of compounds instead of reusing.
2. Try to solve everything through preconditions without a separate operator.
3. Do not specify fallback and leave the behavior “without exit”.
4. Place wide branches above narrow ones and accidentally intercept the plan.
5. Write a root compound with cyclic links without recursion control.

## Mini-checklist before commit

1. All `task:` IDs exist among `htnCompound`.
2. For all new `!type:...` there is a corresponding C# data definition.
3. `HTN` entity component receives the correct `rootTask`.
4. Blackboard keys use the correct types (`!type:Single`, `!type:Bool`, ...).
5. There is a fallback branch.
