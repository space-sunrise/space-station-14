# Skill Authoring Policy

## Scope

This repository uses four skill trees:

- `.agent/skills` is the source of truth (Antigravity format).
- `.agents/skills` is the Codex compatibility layer.
- `.claude/skills` is the Claude Code compatibility layer.
- `.cursor/skills` is the Cursor compatibility layer.

## Required Rule

For every skill in `.agent/skills/<skill-name>`, keep matching bridge files:

- `.agents/skills/<skill-name>/SKILL.md`
- `.claude/skills/<skill-name>/SKILL.md`
- `.cursor/skills/<skill-name>/SKILL.md`

When creating, updating, renaming, or deleting a skill in `.agent/skills`, apply the same change in `.agents/skills`, `.claude/skills`, and `.cursor/skills` in the same pull request.

## Bridge Contract

Each Codex bridge SKILL file must contain:

- `name`: exact `<skill-name>` folder name in hyphen-case.
- `description`: synchronized copy of canonical description from `.agent/skills/<skill-name>/SKILL.md`.
- `metadata.source_skill`: `../../../.agent/skills/<skill-name>/SKILL.md`.

Each Claude bridge SKILL file must contain:

- `name`: exact `<skill-name>` folder name in hyphen-case.
- `description`: synchronized copy of canonical description from `.agent/skills/<skill-name>/SKILL.md`.
- A reference in the markdown body to `../../../.agents/skills/<skill-name>/SKILL.md`.

Each Cursor bridge SKILL file must contain:

- `name`: exact `<skill-name>` folder name in hyphen-case.
- `description`: synchronized copy of canonical description from `.agent/skills/<skill-name>/SKILL.md`.
- A reference in the markdown body to `../../../.claude/skills/<skill-name>/SKILL.md`.

## PR Checklist Gate

A PR is incomplete if any skill exists in `.agent/skills` without matching bridges in `.agents/skills`, `.claude/skills`, and `.cursor/skills`.

Run this check before pushing:

`pwsh ./.agents/skills/check-skill-bridges.ps1`
