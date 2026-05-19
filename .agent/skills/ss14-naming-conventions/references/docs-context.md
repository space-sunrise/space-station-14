# Docs Context (Naming)

## The role of documentation

1. Documentation helps to formalize the terms and general intent.
2. The naming standard is fixed according to the current code, and not according to historical examples in articles.
3. If there is a conflict between docs and code, the code takes precedence.

## What documents to consider

1. `conventions` (updated 2026-01-19).
What to take: agreements on `PascalCase/camelCase/kebab-case`, YAML conventions, basic localization rules.
Limitation: some partitions reflect a mixed legacy state.

2. `fluent-and-localization`.
What to take: FTL syntax, `ent-*` structure, `.desc`/`.suffix` attributes.
Limitation: does not cover fork tightening on IC/OOC content and length limits.

3. `basic-networking-and-you`.
What to take: the context of shared/server/client contracts and the importance of stable names.
Limitation: this is not a specialized naming directory.

## Practice using docs

1. First, verify the rule using the latest code.
2. Then check the docs to clarify the wording.
3. If the docs give a less strict standard, keep a more strict one, confirmed by the code.

## What not to transfer to working rules

1. Outdated examples older than cutoff without recent confirmation.
2. Fragments from TODO/HACK/FIXME on the topic of naming.
3. Linking to a specific source location.
