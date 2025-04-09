using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.Gravity;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Standing;

public sealed class StandingStateSystem : SharedStandingStateSystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ChangeLayingDownEvent>(OnChangeState);
    }

    private void OnChangeState(ChangeLayingDownEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { AttachedEntity: { Valid: true } uid }
            || !Exists(uid)
            || !TryComp<StandingStateComponent>(args.SenderSession.AttachedEntity, out var standingStateComponent)
            || _gravity.IsWeightless(args.SenderSession.AttachedEntity.Value))
            return;

        if (standingStateComponent.CurrentState == StandingState.Laying)
            TryStandUp(uid, standingStateComponent);
        else
        {
            Fall(uid);
        }
    }
}
