using System.Numerics;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Enums;
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
        var bounds = args.WorldAABB.Enlarged(1f);
        var state = new QueryState(_entries, _bloomVisualsQuery, _sprite, _transform);
        _lightTree.QueryAabb(ref state, CollectLight, args.MapId, bounds);
        return _entries.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("base_haze", _baseHaze);
        _shader.SetParameter("haze_divisor", _hazeDivisor / Strength);
        handle.UseShader(_shader);

        foreach (var entry in _entries)
        {
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
        if (!state.BloomVisualsQuery.TryComp(value.Uid, out var bloomVisuals))
            return true;

        var (pointLight, transform) = value;
        var maskTexture = state.Sprite.Frame0(bloomVisuals.Mask);
        var maskOffset = bloomVisuals.Offset - new Vector2(maskTexture.Width, maskTexture.Height) / (2f * EyeManager.PixelsPerMeter);
        var (_, _, worldMatrix) = state.Transform.GetWorldPositionRotationMatrix(transform);
        state.Entries.Add(new LightingOverlayEntry(
            worldMatrix,
            maskTexture,
            maskOffset,
            pointLight.Color * bloomVisuals.Color));
        return false;
    }

    private readonly record struct QueryState(
        List<LightingOverlayEntry> Entries,
        EntityQuery<BloomOverlayVisualsComponent> BloomVisualsQuery,
        SpriteSystem Sprite,
        TransformSystem Transform);
}

public readonly record struct LightingOverlayEntry(
    Matrix3x2 WorldMatrix,
    Texture MaskTexture,
    Vector2 MaskOffset,
    Color Color);
