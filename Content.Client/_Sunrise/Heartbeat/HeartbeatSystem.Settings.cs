using Content.Shared._Sunrise.Heartbeat;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;

namespace Content.Client._Sunrise.Heartbeat;

public sealed class HeartbeatSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

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
        RaiseNetworkEvent(new HeartbeatOptionsChangedEvent(option));
    }
}
