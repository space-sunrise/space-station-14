using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._Sunrise.Shaders.Bloom;

/// <summary>
/// Draws a bloom mask for compatible point lights.
/// </summary>
public sealed class LightingOverlay<TMarker> : Overlay
{
    private readonly ShaderInstance _shader;
    private readonly Texture _maskTexture;
    private readonly Vector2 _maskOffset;
    private readonly float _baseHaze;
    private readonly float _hazeDivisor;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    public override bool RequestScreenTexture => true;

    public IReadOnlyList<LightingOverlayEntry> Entries = [];
    public bool Enabled;
    public float Strength = 1f;

    public LightingOverlay(
        IPrototypeManager prototypeManager,
        SpriteSystem spriteSystem,
        SpriteSpecifier mask,
        Vector2 maskOffset,
        int zIndex,
        float baseHaze,
        float hazeDivisor,
        ProtoId<ShaderPrototype> shaderPrototype)
    {
        _shader = prototypeManager.Index(shaderPrototype).InstanceUnique();
        _maskTexture = spriteSystem.Frame0(mask);
        _maskOffset = maskOffset - new Vector2(_maskTexture.Width, _maskTexture.Height) / (2f * EyeManager.PixelsPerMeter);
        _baseHaze = baseHaze;
        _hazeDivisor = hazeDivisor;
        ZIndex = zIndex;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return Enabled && Entries.Count > 0;
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

        foreach (var entry in Entries)
        {
            if (entry.MapId != args.MapId || !bounds.Contains(entry.WorldPosition))
                continue;

            handle.SetTransform(entry.WorldMatrix);
            handle.DrawTexture(_maskTexture, _maskOffset, entry.Color);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }
}

public sealed class PointLightingOverlayMarker;

public readonly record struct LightingOverlayEntry(
    MapId MapId,
    Matrix3x2 WorldMatrix,
    Vector2 WorldPosition,
    Color Color);
