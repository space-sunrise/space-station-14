# Docs Context (Atmos Core)

## The role of documentation

1. Use docs as design context and terminology.
2. Check each rule against the current code before transferring it into practice.
3. If the docs contradict the code, choose the code.

## Current docs pages

1. `Atmospherics` (department overview) - `2026-02-01` was updated.
2. `Atmospherics Design Choices` — `2026-02-04` was updated.
3. `PR Guidelines` for atmosphere - `2026-01-19` was updated.
4. Proposals (`atmos-rework`, `station-air-recirculation`, `inverse-pneumatic-valves`) - useful as roadmaps/ideas, not as a fact of the current implementation.

## What is important for architecture

1. The priority of gameplay and readability over “ideal physics” is clearly declared.
2. Design choices describe the risk of depending on implementation details (especially the order of devices).
3. The guidelines set out a mandatory approach: document subsystems, avoid god-method, support configurability of stages.
4. The docs emphasize the need for time-budget-aware atmospheric processing.

## Limitations docs

1. Proposal documents contain materials with legacy/roadmap status.
2. Some ideas have not yet been implemented or have been partially implemented.
3. Do not transfer proposal mechanics to the rules without confirmation using the latest code.

## Application practice

1. First consider the code implementation of the stage.
2. Then use docs only to explain “why it is” and not “how exactly it works now”.
3. Fix all controversial areas in `rejected-snippets.md` before implementation.
