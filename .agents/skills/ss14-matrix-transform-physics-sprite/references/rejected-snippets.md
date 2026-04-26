# Rejected Snippets

| Class/method | What's found | Reason for rejection | Date/signal |
|---|---|---|---|
| `TransformComponent.GetWorldPositionRotationMatrixWithInv(EntityQuery<TransformComponent>)` | Internal passage along the parent chain with manual assembly of world/inv-world matrices | Lines in the key block are older than cutoff; not to be used as a modern standard | 2021-11-26, 2022-01-25, 2022-02-02 |
| `ChunkingSystem` (block with `GetInvWorldMatrix(...).TransformBox(...)`) | Converting bounds to chunk-tree locale | Matrix transition line is older than cutoff | 2023-11-28 |
| `SharedBroadphaseSystem` (grid-vs-grid chunk block) | Matrix `TransformBox` for chunk intersections | There is a problematic TODO near the matrix section, plus some lines older than cutoff | TODO: `AddPair ... O(n)` + lines 2023-05-28 |
| `SpriteComponent.Layer.GetLayerDrawMatrix(...)` | Rotating layer-matrix for RSI directions | Explicit TODO/comment about the extra matrix transformation in this section | TODO RENDERING + remark about unnecessary matrix transformation |
| `StationAiVisionSystem` (block `invMatrix.TransformBox(worldBounds)`) | AABB localization for vision-query | Nearby is TODO in the same matrix fragment | TODO about parallel launch next to TransformBox |
| `_Sunrise` case with `WorldMatrix.TransformBox(LocalAABB)` for grid | Working world-AABB transition | Discarded as a duplicate of an already covered upstream pattern FTL/proximity AABB | Pattern duplication, no unique matrix idea |
| Old component wrappers `SpriteComponent`/`TransformComponent` with `[Obsolete]` | Proxying to system methods | Not to take examples: the risk of fixing an outdated API style and mixing the layer of responsibility | Signal: `[Obsolete]` in matrix wrappers |
