# Repository Agent Instructions

This file is the short entrypoint for agents working in this repository. Keep detailed operating rules in `.agents/rules` and detailed skills in `.agents/skills`.

Source-of-truth locations:

- Rules: `.agents/rules`
- Skills: `.agents/skills`
- Compatibility bridges: `.agent`, `.claude`, `.cursor`, `.github`

At the start of a new dialogue, and again after any context compaction, reload the always-on rules from `.agents/rules` and review the available skills in `.agents/skills`. Do not repeat that full reload for every later chat message unless the task changes enough to require new skills or rules.

Before planning, analyzing, or editing, select the rules and skills that apply to the current task. Prefer file extensions and touched subsystems over loose keyword matching. For example, `.cs`, `.yml`, `.yaml`, `.ftl`, and `.swsl` files have explicit SS14 guidance in the rules and skills.

Useful entrypoints:

- Skill and rule preflight: `.agents/rules/ss14-skill-preflight-and-refresh.md`
- Testing requirements: `.agents/rules/ss14-testing-guidelines.md`
- Codebase prefix and edit markers: `.agents/rules/ss14-codebase-prefix-detection.md`
- Interaction architecture pattern: `.agents/rules/ss14-interaction-flow.md`
- Rule authoring policy: `.agents/rules/AUTHORING_POLICY.md`
- Skill authoring policy: `.agents/skills/AUTHORING_POLICY.md`
