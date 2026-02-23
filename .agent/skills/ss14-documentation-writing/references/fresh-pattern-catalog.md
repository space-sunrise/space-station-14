# Fresh Pattern Catalog (Documentation)

> Все записи ниже прошли фильтр свежести: дата изменения не старше 2024-02-20 и без TODO/HACK/FIXME по теме документирования.

| Домен | Паттерн | Подтверждено на свежем примере | Дата | Статус |
|---|---|---|---|---|
| Engine API docs | `summary` + `remarks` + параметризованное описание контракта | `SharedContainerSystem.Remove(...)` | 2026-01-18 | Использовать |
| Engine architecture docs | Документация абстрактного API контейнеров через свойства и методы | `BaseContainer` | 2024-05-26 | Использовать |
| Shared gameplay docs | `summary` на действиях + короткие комментарии на критичных проверках | `MimePowersSystem.OnInvisibleWall(...)` | 2025-09-05 | Использовать |
| Client gameplay docs | `summary` + пояснение сложных преобразований, а не всех строк подряд | `ClickableSystem.CheckClick(...)` | 2025-05-18 | Использовать |
| Server gameplay docs | `summary` + `remarks` для инварианта, важного для вызовов других методов | `AccessOverriderSystem.PrivilegedIdIsAuthorized(...)` | 2025-12-16 | Использовать |
| Large subsystem docs | Разделение большой системы на partial-части с отдельной зоной ответственности | `SharedScp096System` family | 2026-01-24 | Использовать |
| Cross-system docs | Проверка необычных вызовов API в потребителях системы | `Scp096PhotoSystem`, `ArtifactScp096MadnessSystem` + `TryAddTarget(..., true, true)` | 2026-01-08 / 2025-11-29 | Использовать |
| YAML docs | Лаконичные групповые комментарии (1 строка) между блоками прототипов | `salvage reward` catalog groups (`# Rank 2`, `# Rank 3`) | 2025-09-18 | Использовать |
| FTL docs | Корректный разделитель групп в формате `## Group Name` | `battery menu` localization section | 2025-04-28 | Использовать |
| FTL docs hygiene | Проверка валидности заголовков перед merge | контроль на строку `##bombs` в каталоге аплинка | 2026-02-01 | Проверять |

## Короткий вывод

1. `summary` остается главным контрактным форматом документации в C#.
2. Точечные комментарии нужны там, где логика неочевидна (координаты, инварианты, нестандартные ветки).
3. Для YAML/FTL выигрывает лаконичность: короткие разделители и минимум "объяснительных стен".
