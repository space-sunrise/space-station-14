using System.Numerics;
using System.Collections.Generic;

using Content.Shared._Sunrise.SiliconStanding;
using Content.Shared.Movement;
using Content.Shared.Movement.Components;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Log;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;

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
        SubscribeNetworkEvent<SiliconRestingDoAfterEvent>(OnDoAfterResult);
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
    private void OnDoAfterResult(SiliconRestingDoAfterEvent ev)
    {
        var player = _player.LocalSession;

        if (player?.AttachedEntity is not { Valid: true } uid)
            return;
    }
}