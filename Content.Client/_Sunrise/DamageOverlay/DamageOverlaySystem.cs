using Content.Shared._Sunrise.DamageOverlay;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Client._Sunrise.DamageOverlay;

public sealed class DamageOverlaySystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IClientNetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(SunriseCCVars.DamageOverlayEnable, OnDamageOverlayOptionChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.DamageOverlaySelf, OnDamageOverlayOptionChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.DamageOverlayStructures, OnDamageOverlayOptionChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(SunriseCCVars.DamageOverlayEnable, OnDamageOverlayOptionChanged);
        _cfg.UnsubValueChanged(SunriseCCVars.DamageOverlaySelf, OnDamageOverlayOptionChanged);
        _cfg.UnsubValueChanged(SunriseCCVars.DamageOverlayStructures, OnDamageOverlayOptionChanged);
    }

    private void OnDamageOverlayOptionChanged(bool option)
    {
        if (!_netManager.IsConnected)
            return;

        var enable = _cfg.GetCVar(SunriseCCVars.DamageOverlayEnable);
        var enableSelf = _cfg.GetCVar(SunriseCCVars.DamageOverlaySelf);
        var enableStructures = _cfg.GetCVar(SunriseCCVars.DamageOverlayStructures);
        RaiseNetworkEvent(new DamageOverlayOptionEvent(enable, enableSelf, enableStructures));
    }
}
