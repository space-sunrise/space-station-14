using Content.Shared._Sunrise.DamageOverlay;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.DamageOverlay;

public sealed class DamageOverlaySystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(SunriseCCVars.DamageOverlay, OnDamageOverlayOptionChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.DamageOverlayPreset, OnDamageOverlayPresetChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(SunriseCCVars.DamageOverlay, OnDamageOverlayOptionChanged);
        _cfg.UnsubValueChanged(SunriseCCVars.DamageOverlayPreset, OnDamageOverlayPresetChanged);
    }

    private void OnDamageOverlayOptionChanged(bool option)
    {
        RaiseNetworkEvent(new DamageOverlayOptionEvent(option));
    }

    private void OnDamageOverlayPresetChanged(string preset)
    {
        RaiseNetworkEvent(new DamageOverlayPresetChangedEvent(preset));
    }
}
