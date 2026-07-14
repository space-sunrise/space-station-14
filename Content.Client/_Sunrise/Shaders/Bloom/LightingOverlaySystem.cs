using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
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
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private readonly List<LightingOverlayEntry> _entries = [];
    private readonly Dictionary<EntityUid, bool> _visibilityCache = [];
    private static readonly ProtoId<ShaderPrototype> LightingOverlayShader = "SunriseLightingOverlay";
    private const float VisibilityCacheRefreshInterval = 0.2f;
    private LightingOverlay<PointLightingOverlayMarker> _pointOverlay = default!;
    private ConfigurationMultiSubscriptionBuilder _configurationSubscriptions = default!;

    private bool _enabled;
    private bool _visibilityFiltering;
    private float _visibilityCacheRefreshRemaining;

    public override void Initialize()
    {
        base.Initialize();

        _pointOverlay = new LightingOverlay<PointLightingOverlayMarker>(
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
            .OnValueChanged(SunriseCCVars.LightBloomVisibilityFiltering, value => _visibilityFiltering = value, true)
            .OnValueChanged(SunriseCCVars.LightBloomStrength, OnStrengthChanged, true);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_enabled)
            return;

        _entries.Clear();
        var refreshVisibilityCache = _visibilityFiltering
            && (_visibilityCacheRefreshRemaining -= frameTime) <= 0f;

        if (refreshVisibilityCache)
        {
            _visibilityCache.Clear();
            _visibilityCacheRefreshRemaining = VisibilityCacheRefreshInterval;
        }

        var query = EntityQueryEnumerator<BloomOverlayVisualsComponent, PointLightComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var pointLight, out var transform))
        {
            if (!pointLight.Enabled)
                continue;

            if (_visibilityFiltering && !IsLightVisible(uid, refreshVisibilityCache))
            {
                continue;
            }

            var (worldPosition, _, worldMatrix) = _transform.GetWorldPositionRotationMatrix(transform);
            _entries.Add(new LightingOverlayEntry(transform.MapID, worldMatrix, worldPosition, pointLight.Color));
        }

        _pointOverlay.Entries = _entries;
    }

    public override void Shutdown()
    {
        _configurationSubscriptions.Dispose();
        _overlayManager.RemoveOverlay(_pointOverlay);
        _pointOverlay.Dispose();
        base.Shutdown();
    }

    private void OnEnabledChanged(bool value)
    {
        _enabled = value;
        UpdateOverlayRegistration(_pointOverlay, value);
    }

    private void OnStrengthChanged(float value)
    {
        var strength = Math.Clamp(value, 0.1f, 1f);
        _pointOverlay.Strength = strength;
    }

    private bool IsLightVisible(EntityUid light, bool refreshCache)
    {
        if (!refreshCache && _visibilityCache.TryGetValue(light, out var visible))
            return visible;

        visible = _player.LocalEntity is { } player
            && _interaction.InRangeUnobstructed(player, light, range: 30f);
        _visibilityCache[light] = visible;
        return visible;
    }

    private void UpdateOverlayRegistration<TMarker>(LightingOverlay<TMarker> overlay, bool enabled)
    {
        overlay.Enabled = enabled;
        if (enabled && !_overlayManager.HasOverlay(overlay.GetType()))
            _overlayManager.AddOverlay(overlay);
        else if (!enabled && _overlayManager.HasOverlay(overlay.GetType()))
            _overlayManager.RemoveOverlay(overlay);
    }
}
