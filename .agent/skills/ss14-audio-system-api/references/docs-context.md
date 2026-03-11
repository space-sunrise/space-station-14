# Docs Context (Audio API)

## Что из docs реально помогает

1. `prediction-guide` (`2026-02-05`) — лучший источник по predicted-паттернам для аудио API.
2. `basic-networking-and-you` (`2024-05-31`) — полезен для понимания component states и dirty-flow.
3. `conventions` (`2026-01-19`) — подтверждает практику `SoundSpecifier`/`SoundCollectionSpecifier`.
4. `robust-toolbox/midi` (`2024-02-24`) — дает практический фон по buffered audio и «crunchy» сценарию.

## Что считать устаревающим

1. Любые tutorial-страницы с явной меткой `outdated` (например bikehorn-guide) использовать только как исторический onboarding.
2. Даже свежие docs не гарантируют 1:1 соответствие runtime-поведению API, особенно в сетевом и audio-layer коде.

## Правило применения

1. Для API-контрактов сначала смотри код и дату строки (`blame`).
2. Затем используй docs, чтобы обосновать intent и ограничения.
3. Если docs рекомендуют старый подход, фиксируй это в `rejected-snippets.md` и не поднимай в паттерны.
