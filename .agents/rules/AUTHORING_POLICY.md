# Rule Authoring Policy

## Scope

This repository uses five rule trees:

- `.agents/rules` is the source of truth and stores full rule content.
- `.agent/rules` is the Antigravity compatibility layer.
- `.claude/rules` is the Claude Code compatibility layer.
- `.cursor/rules` is the Cursor compatibility layer.
- `.github/rules` is the GitHub Copilot compatibility layer.

## Required Rule

For every rule file in `.agents/rules/<rule-name>.md`, keep matching bridge files:

- `.agent/rules/<rule-name>.md`
- `.claude/rules/<rule-name>.md`
- `.cursor/rules/<rule-name>.md`
- `.github/rules/<rule-name>.md`

When creating, updating, renaming, or deleting a rule in `.agents/rules`, apply the same bridge
change in all bridge trees in the same pull request.

## Bridge Contract

Each Antigravity bridge rule file must contain:

- `trigger`: synchronized copy of canonical trigger from `.agents/rules/<rule-name>.md`.
- `metadata.source_rule`: `../../../.agents/rules/<rule-name>.md`.
- A reference in the markdown body to `../../../.agents/rules/<rule-name>.md`.

Each Claude bridge rule file must contain:

- `trigger`: synchronized copy of canonical trigger from `.agents/rules/<rule-name>.md`.
- A reference in the markdown body to `../../../.agents/rules/<rule-name>.md`.

Each Cursor bridge rule file must contain:

- `trigger`: synchronized copy of canonical trigger from `.agents/rules/<rule-name>.md`.
- A reference in the markdown body to `../../../.agents/rules/<rule-name>.md`.

Each GitHub Copilot bridge rule file must contain:

- `trigger`: synchronized copy of canonical trigger from `.agents/rules/<rule-name>.md`.
- `metadata.source_rule`: `../../../.agents/rules/<rule-name>.md`.
- A reference in the markdown body to `../../../.agents/rules/<rule-name>.md`.

## PR Checklist Gate

A PR is incomplete if any rule exists in `.agents/rules` without matching bridges in
`.agent/rules`, `.claude/rules`, `.cursor/rules`, and `.github/rules`.

Run this check before pushing:

`pwsh ./.agents/rules/check-rule-bridges.ps1`
