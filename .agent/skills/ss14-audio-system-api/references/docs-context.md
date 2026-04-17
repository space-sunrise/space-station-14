# Docs Context (Audio API)

## What docs really help

1. `prediction-guide` (`2026-02-05`) - the best source for predicted patterns for audio API.
2. `basic-networking-and-you` (`2024-05-31`) - useful for understanding component states and dirty-flow.
3. `conventions` (`2026-01-19`) - confirms the practice of `SoundSpecifier`/`SoundCollectionSpecifier`.
4. `robust-toolbox/midi` (`2024-02-24`) - gives a practical background on buffered audio and “crunchy” script.

## What is considered obsolete

1. Any tutorial pages with an explicit label `outdated` (for example bikehorn-guide) should only be used as historical onboarding.
2. Even the latest docs do not guarantee 1:1 compliance with the runtime behavior of the API, especially in network and audio-layer code.

## Rule of application

1. For API contracts, first look at the code and date of the line (`blame`).
2. Then use docs to justify the intent and restrictions.
3. If the docs recommend the old approach, record it in `rejected-snippets.md` and don’t add it to patterns.
