# Docs Context (VirtualController API)

## Роль docs для API

1. Docs нужны для понятий и общих правил, но не заменяют код.
2. API-факты фиксируй по текущей реализации контроллеров и physics system.
3. При конфликте docs и runtime-кода выбирай runtime-код.

## Релевантные страницы

1. `robust-toolbox/transform/physics.md` — дата `2023-09-21`.
Что брать: общую идею physics pipeline и место VirtualControllers.
Ограничение: устаревшая страница, не использовать как источник точного API-контракта.

2. `ss14-by-example/prediction-guide.md` — дата `2026-02-04`.
Что брать: prediction-практики, ожидания по повторным прогонам, типичные mispredict-ловушки.
Ограничение: примеры из гайда все равно нужно сверять с актуальными системами движения.

3. `ss14-by-example/basic-networking-and-you.md` — дата `2024-05-31`.
Что брать: правила dirty/component state для shared/predicted API.
Ограничение: это базовый networking-слой, а не полный справочник movement-контроллеров.

4. `general-development/codebase-info/conventions.md` — дата `2026-01-19`.
Что брать: правила по reusable API, dirty/field-delta, shared naming conventions.
Ограничение: использовать как style-guideline, а не как runtime-поведение.

## Практика применения

1. Сначала выбирай метод по `fresh-pattern-catalog.md`.
2. Потом проверяй docs только для уточнения intent.
3. Если docs предлагают старый паттерн, переноси его в `rejected-snippets.md`, а не в рабочие правила.
