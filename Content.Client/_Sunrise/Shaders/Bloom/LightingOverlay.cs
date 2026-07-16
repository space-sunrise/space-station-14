using System.Numerics;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Shaders.Bloom;

/// <summary>
/// Draws a bloom mask for compatible point lights.
/// </summary>
public sealed class PointLightingOverlay : Overlay
{
    private readonly EntityQuery<BloomOverlayVisualsComponent> _bloomVisualsQuery;
    private readonly LightTreeSystem _lightTree;
    private readonly ShaderInstance _shader;
    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;
    private readonly float _baseHaze;
    private readonly float _hazeDivisor;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    public override bool RequestScreenTexture => true;

    private readonly List<LightingOverlayEntry> _entries = [];
    public float Strength;

    public PointLightingOverlay(
        LightTreeSystem lightTree,
        IPrototypeManager prototypeManager,
        SpriteSystem spriteSystem,
        TransformSystem transform,
        EntityQuery<BloomOverlayVisualsComponent> bloomVisualsQuery,
        int zIndex,
        float baseHaze,
        float hazeDivisor,
        float strength,
        ProtoId<ShaderPrototype> shaderPrototype)
    {
        _lightTree = lightTree;
        _shader = prototypeManager.Index(shaderPrototype).InstanceUnique();
        _sprite = spriteSystem;
        _transform = transform;
        _bloomVisualsQuery = bloomVisualsQuery;
        _baseHaze = baseHaze;
        _hazeDivisor = hazeDivisor;
        Strength = strength;
        ZIndex = zIndex;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _entries.Clear();
        var state = new QueryState(_entries, _bloomVisualsQuery, _sprite, _transform, args.MapId);
        _lightTree.QueryAabb(ref state, CollectLight, args.MapId, args.WorldAABB);
        return _entries.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        var bounds = args.WorldAABB.Enlarged(5f);

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("base_haze", _baseHaze);
        _shader.SetParameter("haze_divisor", _hazeDivisor / Strength);
        handle.UseShader(_shader);

        foreach (var entry in _entries)
        {
            if (entry.MapId != args.MapId || !bounds.Contains(entry.WorldPosition))
                continue;

            handle.SetTransform(entry.WorldMatrix);
            handle.DrawTexture(entry.MaskTexture, entry.MaskOffset, entry.Color);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        base.DisposeBehavior();
    }

    private static bool CollectLight(ref QueryState state, in ComponentTreeEntry<PointLightComponent> value)
    {
        if (!state.BloomVisualsQuery.HasComp(value.Uid))
            return true;

        var (pointLight, transform) = value;
        var bloomVisuals = state.BloomVisualsQuery.GetComponent(value.Uid);
        var maskTexture = state.Sprite.Frame0(bloomVisuals.Mask);
        var maskOffset = bloomVisuals.Offset - new Vector2(maskTexture.Width, maskTexture.Height) / (2f * EyeManager.PixelsPerMeter);
        var (worldPosition, _, worldMatrix) = state.Transform.GetWorldPositionRotationMatrix(transform);
        state.Entries.Add(new LightingOverlayEntry(
            state.MapId,
            worldMatrix,
            worldPosition,
            maskTexture,
            maskOffset,
            pointLight.Color * bloomVisuals.Color));
        return true;
    }

    private readonly record struct QueryState(
        List<LightingOverlayEntry> Entries,
        EntityQuery<BloomOverlayVisualsComponent> BloomVisualsQuery,
        SpriteSystem Sprite,
        TransformSystem Transform,
        MapId MapId);
}

public readonly record struct LightingOverlayEntry(
    MapId MapId,
    Matrix3x2 WorldMatrix,
    Vector2 WorldPosition,
    Texture MaskTexture,
    Vector2 MaskOffset,
    Color Color);
