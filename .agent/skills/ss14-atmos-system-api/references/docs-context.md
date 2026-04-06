# Docs Context (Atmos API)

## What does the docs provide for the API level

1. Formulates the expected behavior of devices: intuitiveness, flow from high pressure to low, avoidance of hidden implementation details.
2. Highlights the risks of update-order dependencies between devices.
3. Requires documentation of public API and mathematical derivation for complex formulas.

## Useful pages and freshness

1. `Atmospherics` — `2026-02-01`.
2. `Atmospherics Design Choices` — `2026-02-04`.
3. `PR Guidelines` — `2026-01-19`.
4. Proposal pages are a roadmap, not a guarantee of implementation.

## How to use with code

1. Take the API method from the code and check the actual behavior in the consumers.
2. Check with design choices so as not to assign implementation detail as gameplay mechanics.
3. If the docs promise more than the code does now, record it in `rejected-snippets.md`.

## Frequent discrepancies

1. Proposal materials describe future flow-based scenarios, but part of the API is still legacy.
2. Not all historical public methods are equally reliable for new systems.
3. Always check the TODO/age of a string before moving it to a new API template.
