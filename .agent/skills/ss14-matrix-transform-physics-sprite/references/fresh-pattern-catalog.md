# Fresh Pattern Catalog

| Class/method | Pattern | Why is it useful | Client/server | Date by blame | Status |
|---|---|---|---|---|---|
| `Matrix3Helpers.CreateTransform(...)` | Canonical assembly of direct matrix `scale * rotate * translate` | Gives a uniform and predictable direct transition to world-space | Shared (engine) | 2025-11-10 | Use |
| `Matrix3Helpers.CreateInverseTransform(...)` | Canonical inversion `translate^-1 * rotate^-1 * scale^-1` | Safe reverse world->local transition without manual character errors | Shared (engine) | 2024-06-02 | Use |
| `SharedPhysicsSystem.GetRelativePhysicsTransform(Transform, relative)` | Converting physical transform to ref-frame broadphase | Removes the risk of incorrect angle/position during local physicals. requests | Shared (engine) | 2024-08-27 | Use |
| `SharedPhysicsSystem.GetRelativePhysicsTransform(entity, relative)` | Transition of world position/angle of entity to local physical. frame | Unifies the physics lookup server/client through one API | Shared (engine) | 2024-08-27 | Use |
| `SpriteSystem.SetScale/SetRotation/SetOffset` | Recalculation of `LocalMatrix` after changing sprite transform parameters | Guarantees the integrity of the render matrix and bounds | Client (engine) | 2025-05-10 | Use |
| `SpriteSystem.Render(...)` | Preparing matrices using layer-strategy (`Default/NoRotation/SnapToCardinals`) | Correctly takes into account `NoRotation`/`SnapCardinals` in the final render | Client (engine) | 2025-05-10 | Use |
| `ClickableSystem.CheckClick(...)` | Chain `world -> entity inverse -> sprite inverse -> layer inverse` | Accurate click hit in pixel/layer locale | Client (upstream) | 2024-08-09 | Use |
| `AmbientOcclusionOverlay.Draw(...)` | `worldMatrix * worldToTextureMatrix` before rendering AO/stencil | Correct world->render-target transition for overlay pipeline | Client (upstream) | 2025-06-24 | Use |
| `ShuttleNavControl.Draw(...)` | Explicit composition `grid/world -> shuttle -> view` | Stable navigation UI mathematics when rotating/zooming | Client (upstream) | 2024-08-21 | Use |
| `DockingSystem.CanDock(...)` | `inverse(stationDock) * gridDock` + `TransformBox` | Checks docking-AABB in the right space without mixing frames | Server (upstream) | 2024-08-26 | Use |
| `ShuttleSystem.TryGetFTLProximity(...)` | Expansion and union world-AABB via `GetWorldMatrix(...).TransformBox(...)` | FTL safe zone taking into account neighboring meshes and their transform | Server (upstream) | 2024-08-25 | Use |
| `FieldOfViewSetAlphaOverlay.Draw(...)` | Translating `worldBounds` to component-tree locale via `InvWorldMatrix.TransformBox` | Reduces noise in FOV-query and keeps correct ref-frame | Client (fork-unique) | 2025-10-10 | Use |
| `HitscanRicochetSystem.OnRicochetPierce(...)` | For direction, zero `M31/M32` before `Vector2.Transform` | Eliminates false vector shift during ricochet | Shared/Server (fork-unique) | 2025-12-26 | Use |
