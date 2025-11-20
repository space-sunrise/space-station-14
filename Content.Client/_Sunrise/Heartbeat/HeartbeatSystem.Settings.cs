using Content.Shared._Sunrise.Heartbeat;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Client._Sunrise.Heartbeat;

public sealed class HeartbeatSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IClientNetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(SunriseCCVars.PlayHeartBeatSound, OnOptionsChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(SunriseCCVars.PlayHeartBeatSound, OnOptionsChanged);
    }

    private void OnOptionsChanged(bool option)
    {
        if (!_netManager.IsConnected)
            return;

        RaiseNetworkEvent(new HeartbeatOptionsChangedEvent(option));
    }
}
