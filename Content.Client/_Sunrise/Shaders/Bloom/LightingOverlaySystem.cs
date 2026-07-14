using Content.Shared._Sunrise.SunriseCCVars;
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
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private static readonly ProtoId<ShaderPrototype> LightingOverlayShader = "SunriseLightingOverlay";
    private PointLightingOverlay _pointOverlay = default!;
    private ConfigurationMultiSubscriptionBuilder _configurationSubscriptions = default!;

    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();

        _pointOverlay = new PointLightingOverlay(
            _prototypeManager,
            _sprite,
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

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_enabled)
            return;

        _pointOverlay.Entries.Clear();
        var query = EntityQueryEnumerator<BloomOverlayVisualsComponent, PointLightComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var pointLight, out var transform))
        {
            if (!pointLight.Enabled)
                continue;

            var (worldPosition, _, worldMatrix) = _transform.GetWorldPositionRotationMatrix(transform);
            _pointOverlay.Entries.Add(new LightingOverlayEntry(transform.MapID, worldMatrix, worldPosition, pointLight.Color));
        }
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
        _enabled = value;
        UpdateOverlayRegistration(value);
    }

    private void OnStrengthChanged(float value)
    {
        var strength = Math.Clamp(value, 0.1f, 1f);
        _pointOverlay.Strength = strength;
    }

    private void UpdateOverlayRegistration(bool enabled)
    {
        _pointOverlay.Enabled = enabled;
        if (enabled && !_overlayManager.HasOverlay(_pointOverlay.GetType()))
            _overlayManager.AddOverlay(_pointOverlay);
        else if (!enabled && _overlayManager.HasOverlay(_pointOverlay.GetType()))
            _overlayManager.RemoveOverlay(_pointOverlay);
    }
}
