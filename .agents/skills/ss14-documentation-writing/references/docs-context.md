# Docs Context (Documentation)

## Role of the docs repository

1. Read `docs` before creating rules.
2. Use docs for intent and terms, and code as the final source of behavior.
3. Always remember that some pages may be out of date.

## Key pages and relevance

1. `conventions` (updated 2026-01-19).
What to take: the requirement to use XML docs (`summary`), the “comment why” approach, YAML and localization conventions.
Limitation: the project contains legacy code that does not fully comply with this standard.

2. `fluent-and-localization` (updated 2026-01-12).
What to take: Fluent principles, key structure, and the role of FTL as a single layer of custom text.
Limitation: the page is not a strict style-guide on the frequency and format of group comments.

3. `forking` (updated 2026-02-07).
What to take: short explanations of the “why” of fork changes.
Limitation: This is not a guide to detailed subsystem documentation.

4. `yaml-crash-course` (updated 2023-09-11).
What to take: Basic YAML syntax and basic comment mechanics.
Limitation: the document is old (over 2 years old), so should not be used as a source of strict design rules.

## Practice using docs

1. First, verify the rule using the latest code.
2. Then clarify the wording through the docs pages.
3. If the docs and the code differ, the code takes precedence.
4. If the docs give a general recommendation, but the code shows a more strict working pattern, record the more strict version in the skill.
