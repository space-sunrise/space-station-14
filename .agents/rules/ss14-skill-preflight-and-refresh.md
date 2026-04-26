---
trigger: always_on
---

# Rule: Skill and rule preflight

This rule is mandatory for any task in this repository.

## 1. When to reload rules and skills

Reload the always-on rules from `.agents/rules` and review the available skills in `.agents/skills`:

1. At the start of a new dialogue.
2. After any context compaction or session summary.
3. When the task changes enough that a new file type, subsystem, or kind of work is involved.

Do not perform a full rules-and-skills reload for every later chat message in the same dialogue when the task has not materially changed.

## 2. Source-of-truth paths

Use these paths directly instead of searching the repository:

1. Rules source of truth: `.agents/rules`.
2. Skills source of truth: `.agents/skills`.
3. Compatibility bridge trees: `.agent`, `.claude`, `.cursor`, `.github`.

When a bridge file points to a source file under `.agents`, load the `.agents` source file and treat it as authoritative.

## 3. How to select skills

Prefer file extensions and concrete subsystems over loose text matching.

If a task touches these file extensions, load the listed skills before planning or editing:

| File extension | Required skills |
| --- | --- |
| `.cs` | `ss14-ecs-components`, `ss14-ecs-entities`, `ss14-ecs-prototypes`, `ss14-ecs-systems`, `ss14-events`, `ss14-prediction` |
| `.yml`, `.yaml` | `ss14-naming-conventions`, `ss14-ecs-prototypes`, `ss14-upstream-maintenance` |
| `.ftl` | `ss14-naming-conventions`, `ss14-ecs-prototypes`, `ss14-upstream-maintenance`, `ss14-localization-strings` |
| `.swsl` | `ss14-naming-conventions`, `ss14-ecs-prototypes`, `ss14-upstream-maintenance` |

Also load subsystem-specific skills when the affected area is clear from file paths, prototypes, APIs, or the requested behavior. Examples:

1. Audio behavior: prefer `ss14-audio-system-api`; use `ss14-audio-system-core` only for deeper architecture or internals.
2. UI work: use the relevant `ss14-ui-*` skill for the UI technology being changed.
3. Networking or prediction-sensitive behavior: include `ss14-netcode`, `ss14-pvs`, or `ss14-prediction` as appropriate.
4. Database or migration work: include `ss14-databases` or `ss14-migrations`.

For large C# changes over roughly 300 lines, include `ss14-documentation-writing`.

For C# code in frequently-called events, `Update()` loops, networking, prediction, or other hot paths, include `ss14-standard-optimizations`.

If the task is to write a plan, include the skills that would be required to execute that plan.

## 4. How to apply selected skills

Selected skills are mandatory implementation constraints for the current task, not optional advice.

Keep the set focused. Do not load every skill that contains a vaguely related word; load the skills tied to the touched extensions, subsystem, or behavior.

If a skill contains inaccurate or outdated information that affects the current work, correct that skill during the task. After doing so, tell the user that the skill was updated intentionally as part of the repository workflow and that the change should not be deleted as accidental noise.

## 5. Temporary working notes

For large investigations or large planned code changes, create a temporary notes file and record only durable task facts:

1. Decisions made.
2. Important code locations.
3. Relevant commands and results.
4. Risks, open questions, and constraints.
5. Facts needed to resume after context compaction.

Delete the temporary notes file before finishing the task.
