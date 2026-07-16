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
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private static readonly ProtoId<ShaderPrototype> LightingOverlayShader = "SunriseLightingOverlay";
    private EntityQuery<BloomOverlayVisualsComponent> _bloomVisualsQuery;
    private PointLightingOverlay _pointOverlay = default!;
    private ConfigurationMultiSubscriptionBuilder _configurationSubscriptions = default!;

    public override void Initialize()
    {
        base.Initialize();

        _bloomVisualsQuery = GetEntityQuery<BloomOverlayVisualsComponent>();
        _pointOverlay = new PointLightingOverlay(
            _lightTree,
            _prototypeManager,
            _sprite,
            _transform,
            _bloomVisualsQuery,
            BloomOverlayVisualsComponent.PointMask,
            BloomOverlayVisualsComponent.PointOffset,
            (int) DrawDepth.Effects,
            0.8f,
            0.05f,
            LightingOverlayShader);

        _configurationSubscriptions = _configuration.SubscribeMultiple()
            .OnValueChanged(SunriseCCVars.LightBloomEnabled, OnEnabledChanged, true)
            .OnValueChanged(SunriseCCVars.LightBloomStrength, OnStrengthChanged, true);
    }

    public override void Shutdown()
    {
        _configurationSubscriptions.Dispose();
        if (_overlayManager.HasOverlay(_pointOverlay.GetType()))
            _overlayManager.RemoveOverlay(_pointOverlay);
        _pointOverlay.Dispose();
        base.Shutdown();
    }

    private void OnEnabledChanged(bool value)
    {
        _pointOverlay.Enabled = value;
        UpdateOverlayRegistration();
    }

    private void OnStrengthChanged(float value)
    {
        var strength = Math.Clamp(value, 0.1f, 1f);
        _pointOverlay.Strength = strength;
    }

    private void UpdateOverlayRegistration()
    {
        if (!_overlayManager.HasOverlay(_pointOverlay.GetType()))
            _overlayManager.AddOverlay(_pointOverlay);
    }
}
