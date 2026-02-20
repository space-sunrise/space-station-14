# Skill Authoring Policy

## Scope

This repository uses two skill trees:

- `.agent/skills` is the source of truth (Antigravity format).
- `.agents/skills` is the Codex compatibility layer.

## Required Rule

For every skill in `.agent/skills/<skill-name>`, keep a matching bridge file:

- `.agents/skills/<skill-name>/SKILL.md`

When creating, updating, renaming, or deleting a skill in `.agent/skills`, apply the same change in `.agents/skills` in the same pull request.

## Bridge Contract

Each bridge SKILL file must contain:

- `name`: exact `<skill-name>` folder name in hyphen-case.
- `description`: synchronized copy of canonical description from `.agent/skills/<skill-name>/SKILL.md`.
- `metadata.source_skill`: `../../../.agent/skills/<skill-name>/SKILL.md`.

## PR Checklist Gate

A PR is incomplete if any skill exists in `.agent/skills` without a matching bridge in `.agents/skills`.

Run this check before pushing:

`pwsh ./.agents/skills/check-skill-bridges.ps1`
