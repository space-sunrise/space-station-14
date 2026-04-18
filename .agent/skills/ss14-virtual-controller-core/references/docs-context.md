# Docs Context (VirtualController Core)

## The role of documentation

1. Use Docs for terminology, intent and diagnostics.
2. Always check the behavior of the system using the current code.
3. If there is a conflict between docs vs code, choose code.

## Relevant pages

1. `robust-toolbox/transform/physics.md` — date `2023-09-21`.
Purpose: general picture of the physics pipeline and the place of VirtualControllers.
Trust limitation: the page is out of date, there is a TODO, use only as a high-level intent.

2. `ss14-by-example/prediction-guide.md` — date `2026-02-04`.
Purpose: practice prediction, replay/re-sim and guard patterns.
Reliance limitation: guidance on methodology, but always check specific examples against current systems.

3. `ss14-by-example/basic-networking-and-you.md` — date `2024-05-31`.
Purpose: `Dirty`/network fields/ComponentState for shared predict scripts.
Trust constraint: Useful as a basic network model, but not a replacement for reading runtime code.

## Practice using docs

1. First, find the latest implementation in the code (after the cutoff `2024-02-20`).
2. Then check the docs for terminology and explanations.
3. If the docs are being pushed into a legacy approach, record this in `rejected-snippets.md`.
