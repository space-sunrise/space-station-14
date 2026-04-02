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

namespace Content.Client._Sunrise.SiliconStanding;

public sealed class SiliconStandingSystem : EntitySystem
{
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleStanding, InputCmdHandler.FromDelegate(SendToggleEvent, handle: true)).Register<SiliconStandingSystem>();
        SubscribeLocalEvent<SiliconRestingComponent, ComponentStartup>(OnRestSync);
        SubscribeLocalEvent<SiliconRestingComponent, ComponentShutdown>(OnRestSyncRemove);
    }
    private HashSet<EntityUid> _predictedRest = new();

    private void SendToggleEvent(ICommonSession? session)
    {
        if (!_net.IsConnected)
            return;

        var player = _player.LocalSession;

        if (player?.AttachedEntity is not { Valid: true } uid)
            return;

        if (TryComp<InputMoverComponent>(uid, out var mover))
        {
            mover.WishDir = Vector2.Zero;
            mover.CurTickWalkMovement = Vector2.Zero;
            mover.CurTickSprintMovement = Vector2.Zero;
        }

        _predictedRest.Add(uid);

        RaiseNetworkEvent(new ToggleStandingEvent());
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InputMoverComponent>();

        while (query.MoveNext(out var uid, out var mover))
            if (HasComp<SiliconRestingComponent>(uid) || _predictedRest.Contains(uid))
                mover.WishDir = Vector2.Zero;
    }

    private void OnRestSync(EntityUid uid, SiliconRestingComponent comp, ComponentStartup args)
    {
        _predictedRest.Remove(uid);
    }

    private void OnRestSyncRemove(EntityUid uid, SiliconRestingComponent comp, ComponentShutdown args)
    {
        _predictedRest.Remove(uid);
    }
}