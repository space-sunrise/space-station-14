---
name: ss14-matrix-transform-physics-sprite
description: Highly specialized skill in matrix transformations in SS14: world/grid/local/screen conversions, Sprite render matrices, broadphase-space physics queries, Transform/Physics/Sprite client and server chains.
---

# Matrices for Transform/Physics/Sprite in SS14

Use this skill when the task involves matrices and coordinate space conversion :)
Focus only on the matrix part of `Transform/Physics/Sprite` without duplicating common topics from other skills.

## When to use

Use skill if necessary:

- transfer data between `world/grid/local/screen`;
- correctly build/invert matrices for physics and rendering;
- convert bounds (`TransformBox`) for broadphase, AO/FOV, docking, UI navigation;
- resolve client/server differences in matrix chains.

## Mental model of matrices

1. Separate spaces strictly: `world` -> `entity/grid local` -> `sprite/layer local` -> `screen`.
2. Use paired operations: `CreateTransform` for forward movement, `CreateInverseTransform` for reverse movement.
3. For physics, first select the correct `reference frame` (usually broadphase/grid), then convert the position and angle.
4. For rendering, take into account the sprite modifiers (`NoRotation`, `SnapCardinals`, layer strategy), and only then calculate the draw-matrix.
5. For directions, reset the matrix translation (`M31/M32 = 0`), otherwise you distort the vector ⚠️
6. Work through system methods (`SharedTransformSystem`, `SharedPhysicsSystem`, `SpriteSystem`), rather than manual calculations.

## Patterns

1. Use `Matrix3Helpers.CreateTransform(...)` and `CreateInverseTransform(...)` as a canonical forward/backward transition pair.
2. In physics, get a relative transform through `SharedPhysicsSystem.GetRelativePhysicsTransform(...)` instead of manually subtracting the parent chain.
3. Rebuild `SpriteComponent.LocalMatrix` when changing `Scale/Rotation/Offset` through `SpriteSystem.SetScale/SetRotation/SetOffset`.
4. In the sprite renderer, prepare separate matrices for layer strategies (`Default`, `NoRotation`, `SnapToCardinals`, `UseSpriteStrategy`).
5. In cliques, make a chain `world -> entity local -> sprite local -> layer local` through matrix inversions.
6. In the docking composition, use the formula `inverse(stationDock) * gridDock`, then `TransformBox` for AABB checking.
7. In the UI navigation of radars and maps, make an explicit chain `grid/world -> shuttle -> view`.
8. In AO/FOV, before querying on trees/tiles, transfer world-bounds to the local space of the tree/grid via `GetInvWorldMatrix(...).TransformBox(...)`.
9. In FTL/proximity, expand and merge world-AABB via `GetWorldMatrix(...).TransformBox(...)`, rather than mixing world/local boxes directly.
10. For direction vectors (rays, ricochet), use a matrix without translation (`M31/M32 = 0`) and normalize after transformation.
11. For mixed client/server code, keep a single geometric meaning: the same mathematics, but different points of application.
12. In complex loops, first calculate the base matrices once, then only reuse them in iterations ✅

## Anti-patterns

1. Mix `world`, `grid local`, `entity local`, `screen` in one expression without an explicit transition.
2. Convert directions using a complete matrix with translation (you will get the wrong geometry).
3. Ignore `NoRotation` and `SnapCardinals` when calculating the sprite screen matrix.
4. Bypass system APIs and manually assemble the parent chain of matrices where there are ready-made methods.
5. Use legacy component wrappers as the main API instead of system methods.
6. Invert matrices into a tight-loop without caching if the input parameters do not change during the iteration.
7. Perform broadphase/query in world-space when local tree/grid space is expected.
8. Glue layer `Sprite` matrices and physical transforms without understanding their different reference systems.
9. Draw into this skill general UI/architecture topics without matrix content 🙃

## Client/Server

- `Client`: clicks, sprite rendering, AO/FOV, navigation UI widgets; here the `world -> local -> screen` chains are most often needed.
- `Server`: docking/FTL/physical checks; Correct relative transforms and `TransformBox` for collision/query are critical here.
- `Shared`: basic matrix helpers and geometry unification (`CreateTransform`, `CreateInverseTransform`, relative-physics transform).
- Keep it invariant: mathematics coincides between layers, only the context of application diverges.

## Code examples

### 1) Engine: `Matrix3Helpers.CreateTransform` + `CreateInverseTransform`

```csharp
// Direct transition: local -> world.
var worldMatrix = Matrix3Helpers.CreateTransform(position, angle, scale);

// Reverse transition: world -> local space of the same entity.
var invWorldMatrix = Matrix3Helpers.CreateInverseTransform(position, angle, scale);

// Example: we transfer a point from the world to local.
var localPoint = Vector2.Transform(worldPoint, invWorldMatrix);
```

### 2) Engine: `SharedPhysicsSystem.GetRelativePhysicsTransform(...)`

```csharp
// We get a broadphase-oriented transform, rather than counting it manually through the parent chain.
var (_, broadphaseRot, _, broadphaseInv) = _transform.GetWorldPositionRotationMatrixWithInv(relativeXform);

// Position and angle in local broadphase space.
var localPos = Vector2.Transform(worldTransform.Position, broadphaseInv);
var localRot = worldTransform.Quaternion2D.Angle - broadphaseRot;
var localTransform = new Transform(localPos, localRot);
```

### 3) Engine: `SpriteSystem.SetScale/SetRotation/SetOffset`

```csharp
// After changing scale/rotation/offset, always rebuild LocalMatrix.
sprite.Comp.scale = newScale;
sprite.Comp.LocalMatrix = Matrix3Helpers.CreateTransform(
    in sprite.Comp.offset,
    in sprite.Comp.rotation,
    in sprite.Comp.scale);

// This is how the layer renderer gets a consistent matrix without “drift”.
```

### 4) Engine: `SpriteSystem.Render` and layer strategies

```csharp
// Basic matrix taking into account no-rotation / snap-cardinals.
var entityMatrix = Matrix3Helpers.CreateTransform(
    worldPosition,
    sprite.Comp.NoRotation ? -eyeRotation : worldRotation - cardinal);

// For granular rendering, we calculate individual matrices in advance and choose according to the layer strategy.
var transformDefault = Matrix3x2.Multiply(sprite.Comp.LocalMatrix,
    Matrix3Helpers.CreateTransform(worldPosition, worldRotation));
var transformNoRot = Matrix3x2.Multiply(sprite.Comp.LocalMatrix,
    Matrix3Helpers.CreateTransform(worldPosition, -eyeRotation));
```

### 5) Upstream (client): `ClickableSystem.CheckClick`

```csharp
// 1) Invert the sprite local matrix.
Matrix3x2.Invert(sprite.LocalMatrix, out var invSpriteMatrix);

// 2) We build inverse-transform entities taking into account no-rotation/snap-cardinals.
var entityInv = Matrix3Helpers.CreateInverseTransform(spritePos, correctedRotation);

// 3) Translate world-click into local-space of the sprite and then into layer-space.
var localPos = Vector2.Transform(Vector2.Transform(worldPos, entityInv), invSpriteMatrix);
```

### 6) Upstream (server): `DockingSystem.CanDock`

```csharp
// Docking matrix: convert grid-dock to shuttle-dock system.
var stationDockMatrix = Matrix3Helpers.CreateInverseTransform(stationDockPos, shuttleDockAngle);
var gridDockMatrix = Matrix3Helpers.CreateTransform(gridDockLocalPos, gridDockAngle);
var dockingMatrix = Matrix3x2.Multiply(stationDockMatrix, gridDockMatrix);

// We immediately check the new shuttle AABB in the target reference.
var dockedAabb = dockingMatrix.TransformBox(shuttleAabb);
```

### 7) Upstream (UI client): `ShuttleNavControl.Draw`

```csharp
// Matrix chain: shuttle-local -> world -> view.
var posMatrix = Matrix3Helpers.CreateTransform(selectedCoordinates.Position, selectedRotation);
var shuttleToWorld = Matrix3x2.Multiply(posMatrix, controlledEntityWorldMatrix);
Matrix3x2.Invert(shuttleToWorld, out var worldToShuttle);

// For each grid we build world -> shuttle -> view.
var gridToView = Matrix3x2.Multiply(curGridToWorld, worldToShuttle) * shuttleToView;
```

### 8) Upstream (client): `AmbientOcclusionOverlay.Draw`

```csharp
// AO rendering goes to render-target, so you need world -> texture matrix.
var invMatrix = renderTarget.GetWorldToLocalMatrix(viewportEye, scale);

// We take the world matrix of the entity/grid and multiply it by the invMatrix of the target.
var worldMatrix = xformSystem.GetWorldMatrix(entry.Transform);
var worldToTexture = Matrix3x2.Multiply(worldMatrix, invMatrix);

// Next we draw the geometry in the correct texture-space.
worldHandle.SetTransform(worldToTexture);
```

### 9) Fork-unique (client): `FieldOfViewSetAlphaOverlay.Draw`

```csharp
// For each component-tree, we first translate worldBounds into local tree coordinates.
var boundsLocalToTree = _xform.GetInvWorldMatrix(treeUid).TransformBox(worldBounds);

// Then the query on AABB is executed in the correct space.
treeComp.Tree.QueryAabb(ref state, QueryCallback, boundsLocalToTree, true);
```

### 10) Fork-unique (shared/server): `HitscanRicochetSystem.OnRicochetPierce`

```csharp
// The hit position is translated by a complete inverse matrix.
var invMatrix = _transform.GetInvWorldMatrix(ent.Owner);
var localFrom = Vector2.Transform(worldHitPos, invMatrix);

// The direction is translated by the matrix WITHOUT translation.
var invNoTrans = invMatrix;
invNoTrans.M31 = 0f;
invNoTrans.M32 = 0f;
var localDir = Vector2.Transform(worldDir, invNoTrans).Normalized();

// After calculating the reflection, we return the direction back to world-space,
// again no broadcast.
```

---

Use this skill as a narrow matrix playbook: take only fresh and clean areas, and send questionable/old cases to `rejected`.
