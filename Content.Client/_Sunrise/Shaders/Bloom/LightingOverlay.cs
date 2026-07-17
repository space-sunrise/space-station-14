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
    private const int MaxVisibleLights = 64;

    private readonly EntityQuery<BloomOverlayVisualsComponent> _bloomVisualsQuery;
    private readonly LightTreeSystem _lightTree;
    private readonly Dictionary<BloomMaskKey, BloomMaskData> _maskCache = [];
    private readonly ShaderInstance _shader;
    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;
    private readonly float _baseHaze;
    private readonly float _hazeDivisor;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    public override bool RequestScreenTexture => true;

    private readonly List<BloomLightEntry> _visibleLights = [];
    public float BloomStrength;

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
        BloomStrength = strength;
        ZIndex = zIndex;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (BloomStrength <= 0f)
            return false;

        _visibleLights.Clear();
        var visibleArea = args.WorldAABB.Enlarged(1f);
        var queryState = new BloomLightQueryState(
            _visibleLights,
            _maskCache,
            _bloomVisualsQuery,
            _sprite,
            _transform,
            visibleArea,
            visibleArea.Center);
        _lightTree.QueryAabb(ref queryState, CollectBloomLight, args.MapId, visibleArea);
        return _visibleLights.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("LIGHT_TEXTURE", args.Viewport.LightRenderTarget.Texture);
        _shader.SetParameter("base_haze", _baseHaze);
        _shader.SetParameter("haze_divisor", _hazeDivisor / BloomStrength);
        handle.UseShader(_shader);

        foreach (var light in _visibleLights)
        {
            handle.SetTransform(light.WorldMatrix);
            handle.DrawTexture(light.MaskTexture, light.MaskOffset, light.Color);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        base.DisposeBehavior();
    }

    private static bool CollectBloomLight(ref BloomLightQueryState queryState, in ComponentTreeEntry<PointLightComponent> lightEntry)
    {
        if (!queryState.BloomVisualsQuery.TryComp(lightEntry.Uid, out var bloomVisuals))
            return true;

        var (pointLight, transform) = lightEntry;
        var (worldPosition, _, worldMatrix) = queryState.Transform.GetWorldPositionRotationMatrix(transform);
        if (!queryState.VisibleArea.Contains(worldPosition))
            return true;

        var distanceSquared = Vector2.DistanceSquared(worldPosition, queryState.ViewCenter);
        var replacementIndex = -1;

        if (queryState.VisibleLights.Count >= MaxVisibleLights)
        {
            var farthestIndex = 0;
            var farthestDistanceSquared = queryState.VisibleLights[0].DistanceSquared;
            for (var i = 1; i < queryState.VisibleLights.Count; i++)
            {
                var visibleLight = queryState.VisibleLights[i];
                if (visibleLight.DistanceSquared <= farthestDistanceSquared)
                    continue;

                farthestIndex = i;
                farthestDistanceSquared = visibleLight.DistanceSquared;
            }

            if (distanceSquared >= farthestDistanceSquared)
                return true;

            replacementIndex = farthestIndex;
        }

        var maskKey = new BloomMaskKey(bloomVisuals.MaskSprite, bloomVisuals.MaskOffset);
        if (!queryState.MaskCache.TryGetValue(maskKey, out var mask))
        {
            var texture = queryState.Sprite.Frame0(bloomVisuals.MaskSprite);
            mask = new BloomMaskData(
                texture,
                bloomVisuals.MaskOffset - new Vector2(texture.Width, texture.Height) / (2f * EyeManager.PixelsPerMeter));
            queryState.MaskCache.Add(maskKey, mask);
        }

        var light = new BloomLightEntry(
            worldMatrix,
            mask.Texture,
            mask.Offset,
            pointLight.Color * bloomVisuals.BloomColor,
            distanceSquared);

        if (replacementIndex >= 0)
            queryState.VisibleLights[replacementIndex] = light;
        else
            queryState.VisibleLights.Add(light);

        return true;
    }

    private readonly record struct BloomLightQueryState(
        List<BloomLightEntry> VisibleLights,
        Dictionary<BloomMaskKey, BloomMaskData> MaskCache,
        EntityQuery<BloomOverlayVisualsComponent> BloomVisualsQuery,
        SpriteSystem Sprite,
        TransformSystem Transform,
        Box2 VisibleArea,
        Vector2 ViewCenter);

    private readonly record struct BloomMaskKey(SpriteSpecifier Sprite, Vector2 Offset);

    private readonly record struct BloomMaskData(Texture Texture, Vector2 Offset);

    private readonly record struct BloomLightEntry(
        Matrix3x2 WorldMatrix,
        Texture MaskTexture,
        Vector2 MaskOffset,
        Color Color,
        float DistanceSquared);
}
