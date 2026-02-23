# Rule Authoring Policy

## Scope

This repository uses five rule trees:

- `.agent/rules` is the source of truth.
- `.agents/rules` is the Codex compatibility layer.
- `.claude/rules` is the Claude Code compatibility layer.
- `.cursor/rules` is the Cursor compatibility layer.
- `.github/rules` is the GitHub Copilot compatibility layer.

## Required Rule

For every rule file in `.agent/rules/<rule-name>.md`, keep matching bridge files:

- `.agents/rules/<rule-name>.md`
- `.claude/rules/<rule-name>.md`
- `.cursor/rules/<rule-name>.md`
- `.github/rules/<rule-name>.md`

When creating, updating, renaming, or deleting a rule in `.agent/rules`, apply the same
change in all bridge trees in the same pull request.

## Bridge Contract

Each Codex bridge rule file must contain:

- `trigger`: synchronized copy of canonical trigger from `.agent/rules/<rule-name>.md`.
- `metadata.source_rule`: `../../../.agent/rules/<rule-name>.md`.

Each Claude bridge rule file must contain:

- `trigger`: synchronized copy of canonical trigger from `.agent/rules/<rule-name>.md`.
- A reference in the markdown body to `../../../.agents/rules/<rule-name>.md`.

Each Cursor bridge rule file must contain:

- `trigger`: synchronized copy of canonical trigger from `.agent/rules/<rule-name>.md`.
- A reference in the markdown body to `../../../.claude/rules/<rule-name>.md`.

Each GitHub Copilot bridge rule file must contain:

- `trigger`: synchronized copy of canonical trigger from `.agent/rules/<rule-name>.md`.
- `metadata.source_rule`: `../../../.agent/rules/<rule-name>.md`.
- A reference in the markdown body to `../../../.agent/rules/<rule-name>.md`.

## PR Checklist Gate

A PR is incomplete if any rule exists in `.agent/rules` without matching bridges in
`.agents/rules`, `.claude/rules`, `.cursor/rules`, and `.github/rules`.

Run this check before pushing:

`pwsh ./.agents/rules/check-rule-bridges.ps1`
