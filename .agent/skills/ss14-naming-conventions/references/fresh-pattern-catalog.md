# Fresh Pattern Catalog (Naming)

> Все записи ниже прошли фильтр свежести: дата изменения не старше 2024-02-20 и без TODO/HACK/FIXME по теме нейминга.

| Домен | Паттерн | Подтверждено на свежем примере | Дата | Статус |
|---|---|---|---|---|
| Components | `XxxComponent` + `XxxSystem` с общей базой | `ClickableComponent` + `ClickableSystem` | 2025-05-18 | Использовать |
| Components | Предикативное имя компонента | `ClickableComponent` | 2024-08-09 | Использовать |
| Systems | Система заканчивается на `System` | `EntityPickupAnimationSystem` | 2026-01-08 | Использовать |
| Dependencies | `IGameTiming` -> `_timing` | `SharedIdCardSystem` | 2025-08-23 | Использовать |
| Dependencies | `IRobustRandom` -> `_random` | `DrugOverlaySystem` | 2025-06-27 | Использовать |
| Dependencies | `IPlayerManager` -> `_player` | `DrugOverlaySystem`/`DrunkSystem` | 2025-08-18 | Использовать |
| Dependencies | `TransformSystem` -> `_transform` | `EntityPickupAnimationSystem` | 2026-01-08 | Использовать |
| Dependencies | `EntityWhitelistSystem` -> `_whitelist` | `ChangeNameInContainerSystem` | 2024-10-09 | Использовать |
| YAML components | В прототипе писать `- type: Xxx` без `Component` | `- type: Clickable` | 2025-10-12 | Использовать |
| Prototype IDs | `CamelCase` для ID | `FloorWaterEntity`, `Scp096CryOut` | 2026-01-08 | Использовать |
| Fork IDs | Форк-префикс для fork-only контента | `SunriseAmmunition` | 2026-01-14 | Использовать |
| Prototype fallback | Английский `name/description` в YAML | `name: emit mournful scream` | 2026-01-08 | Использовать |
| Ent localization | `ent-MyEntity` + `.desc` | `ent-BasePart`, `.desc` | 2024-08-30 | Использовать |
| Generic localization | Неструктурные ключи в `kebab-case` | `armable-examine-armed` | 2025-04-25 | Использовать |
| Variables | Приватные поля с `_` | `_overlay`, `_transform`, `_whitelist` | 2026-01-08 | Использовать |
| File naming | C# partial-файлы с осмысленным суффиксом секции | `*.Section.cs` стиль для partial-декомпозиции | 2025-05-18 | Использовать |
| File naming | `snake_case` для YAML/FTL | `water.yml`, `armable.ftl` стиль | 2025-10-12 | Использовать |

## Краткий вывод

1. Базовый связанный нейминг `XxxComponent`/`XxxSystem` стабилен и широко повторяется.
2. Каноничные dependency-алиасы (`_timing`, `_random`, `_transform`, `_player`, `_whitelist`) имеют устойчивое подтверждение.
3. Прототипы и локализация используют строгий split: `CamelCase` ID + `ent-*`/`kebab-case` для FTL.
