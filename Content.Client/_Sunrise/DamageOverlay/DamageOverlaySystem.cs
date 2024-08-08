using Content.Shared._Sunrise.DamageOverlay;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;

namespace Content.Client._Sunrise.DamageOverlay;

public sealed class DamageOverlaySystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(SunriseCCVars.DamageOverlay, OnDamageOverlayOptionChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(SunriseCCVars.DamageOverlay, OnDamageOverlayOptionChanged);
    }

    private void OnDamageOverlayOptionChanged(bool option)
    {
        RaiseNetworkEvent(new DamageOverlayOptionEvent(option));
    }
}
