# Docs Context (Audio Core)

## The role of documentation

1. Use Docs for terms, intent and network context.
2. Confirm real behavior using code.
3. If docs and code disagree, choose code.

## Useful docs pages

1. `prediction-guide` — `2026-02-05`.
Covers predicted API and restrictions for audio (`PlayPredicted`, user-context, first-time prediction).

2. `codebase conventions` — `2026-01-19`.
Fixes general rules for `SoundSpecifier`, API style and working with network state.

3. `basic-networking-and-you` — `2024-05-31`.
Well explains component states and replication, which is directly important for `AudioComponent` and ambient state.

4. `robust-toolbox/midi` — `2024-02-24`.
Useful as a background on buffered audio and OpenAL behavior when buffers are underfilled.

## Pages with restrictions

1. `adding-a-simple-bikehorn` - the file has an explicit mark `outdated`.
Can only be used as a historical tutorial, not as a source of modern audio practices.

## Practice using docs

1. First, check the latest methods in the code (`blame >= 2024-02-19`).
2. Then use docs to explain “why this is so,” and not “how it works exactly now.”
3. If material from the docs leads to the old API model, record it in `rejected-snippets.md`.
