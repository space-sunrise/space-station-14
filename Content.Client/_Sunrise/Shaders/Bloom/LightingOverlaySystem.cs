using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._Sunrise.Shaders.Bloom;

/// <summary>
/// Collects compatible lights and supplies their state to the bloom overlays.
/// </summary>
public sealed class LightingOverlaySystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly LightTreeSystem _lightTree = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private static readonly ProtoId<ShaderPrototype> LightingOverlayShader = "SunriseLightingOverlay";
    private EntityQuery<BloomOverlayVisualsComponent> _bloomVisualsQuery;
    private PointLightingOverlay? _pointOverlay;
    private ConfigurationMultiSubscriptionBuilder _configurationSubscriptions = default!;
    private float _strength = 0.7f;

    public override void Initialize()
    {
        base.Initialize();

        _bloomVisualsQuery = GetEntityQuery<BloomOverlayVisualsComponent>();
        _configurationSubscriptions = _configuration.SubscribeMultiple()
            .OnValueChanged(SunriseCCVars.LightBloomEnabled, OnEnabledChanged, true)
            .OnValueChanged(SunriseCCVars.LightBloomStrength, OnStrengthChanged, true);
    }

    public override void Shutdown()
    {
        _configurationSubscriptions.Dispose();
        if (_pointOverlay is { } overlay)
        {
            if (_overlay.HasOverlay<PointLightingOverlay>())
                _overlay.RemoveOverlay(overlay);

            overlay.Dispose();
            _pointOverlay = null;
        }

        base.Shutdown();
    }

    private void OnEnabledChanged(bool value)
    {
        if (!value)
        {
            if (_pointOverlay is not { } overlay)
                return;

            if (_overlay.HasOverlay<PointLightingOverlay>())
                _overlay.RemoveOverlay(overlay);

            overlay.Dispose();
            _pointOverlay = null;
            return;
        }

        _pointOverlay ??= new PointLightingOverlay(
            _lightTree,
            _prototype,
            _sprite,
            _transform,
            _bloomVisualsQuery,
            BloomOverlayVisualsComponent.PointMask,
            BloomOverlayVisualsComponent.PointOffset,
            (int) DrawDepth.Effects,
            0.8f,
            0.05f,
            _strength,
            LightingOverlayShader);

        if (!_overlay.HasOverlay<PointLightingOverlay>())
            _overlay.AddOverlay(_pointOverlay);
    }

    private void OnStrengthChanged(float value)
    {
        _strength = Math.Clamp(value, 0.1f, 1f);
        if (_pointOverlay is { } overlay)
            overlay.Strength = _strength;
    }
}
