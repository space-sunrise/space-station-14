using Content.Shared._Sunrise.SiliconStanding;
using Content.Shared.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Network;

namespace Content.Client._Sunrise.SiliconStanding;

public sealed class SiliconStandingSystem : EntitySystem
{
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleStanding,
                InputCmdHandler.FromDelegate(SendToggleEvent, handle: true))
            .Register<SiliconStandingSystem>();
    }
    private void SendToggleEvent(ICommonSession? session)
    {
        if (!_net.IsConnected)
            return;

        var player = _player.LocalSession;

        if (player?.AttachedEntity is not { Valid: true } uid)
            return;

        RaiseNetworkEvent(new ToggleStandingEvent());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SiliconStandingSystem>();
    }
}
