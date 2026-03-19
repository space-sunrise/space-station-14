# Fresh Pattern Catalog

| Класс/метод | Паттерн | Почему полезно | Клиент/сервер | Дата по blame | Статус |
|---|---|---|---|---|---|
| `Matrix3Helpers.CreateTransform(...)` | Каноническая сборка прямой матрицы `scale * rotate * translate` | Дает единый и предсказуемый прямой переход в world-space | Shared (движок) | 2025-11-10 | Использовать |
| `Matrix3Helpers.CreateInverseTransform(...)` | Каноническая инверсия `translate^-1 * rotate^-1 * scale^-1` | Безопасный обратный переход world->local без ручных ошибок в знаках | Shared (движок) | 2024-06-02 | Использовать |
| `SharedPhysicsSystem.GetRelativePhysicsTransform(Transform, relative)` | Конверсия физического transform в ref-frame broadphase | Снимает риск неверного угла/позиции при локальных физ. запросах | Shared (движок) | 2024-08-27 | Использовать |
| `SharedPhysicsSystem.GetRelativePhysicsTransform(entity, relative)` | Переход world позиции/угла сущности в локальный физ. frame | Унифицирует сервер/клиент lookup по физике через один API | Shared (движок) | 2024-08-27 | Использовать |
| `SpriteSystem.SetScale/SetRotation/SetOffset` | Пересчет `LocalMatrix` после изменения трансформ-параметров спрайта | Гарантирует целостность render-матрицы и bounds | Client (движок) | 2025-05-10 | Использовать |
| `SpriteSystem.Render(...)` | Подготовка матриц по layer-strategy (`Default/NoRotation/SnapToCardinals`) | Правильно учитывает `NoRotation`/`SnapCardinals` в финальном рендере | Client (движок) | 2025-05-10 | Использовать |
| `ClickableSystem.CheckClick(...)` | Цепочка `world -> entity inverse -> sprite inverse -> layer inverse` | Точное попадание клика в пиксельный/слойный локал | Client (upstream) | 2024-08-09 | Использовать |
| `AmbientOcclusionOverlay.Draw(...)` | `worldMatrix * worldToTextureMatrix` перед отрисовкой AO/stencil | Корректный world->render-target переход для overlay-пайплайна | Client (upstream) | 2025-06-24 | Использовать |
| `ShuttleNavControl.Draw(...)` | Явная композиция `grid/world -> shuttle -> view` | Стабильная математика навигационного UI при поворотах/масштабе | Client (upstream) | 2024-08-21 | Использовать |
| `DockingSystem.CanDock(...)` | `inverse(stationDock) * gridDock` + `TransformBox` | Проверяет docking-AABB в нужном пространстве без смешения frames | Server (upstream) | 2024-08-26 | Использовать |
| `ShuttleSystem.TryGetFTLProximity(...)` | Расширение и union world-AABB через `GetWorldMatrix(...).TransformBox(...)` | Безопасная зона FTL с учетом соседних сеток и их transform | Server (upstream) | 2024-08-25 | Использовать |
| `FieldOfViewSetAlphaOverlay.Draw(...)` | Перевод `worldBounds` в локал component-tree через `InvWorldMatrix.TransformBox` | Снижает шум в FOV-query и держит правильный ref-frame | Client (fork-unique) | 2025-10-10 | Использовать |
| `HitscanRicochetSystem.OnRicochetPierce(...)` | Для направления зануляет `M31/M32` перед `Vector2.Transform` | Исключает ложный сдвиг векторов при рикошете | Shared/Server (fork-unique) | 2025-12-26 | Использовать |
