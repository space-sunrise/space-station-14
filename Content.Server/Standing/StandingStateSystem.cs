using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.Gravity;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Standing;

public sealed class StandingStateSystem : SharedStandingStateSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ChangeLayingDownEvent>(OnChangeState);
        SubscribeLocalEvent<StandingStateComponent, DropHandItemsEvent>(FallOver);
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

    private void FallOver(EntityUid uid, StandingStateComponent component, DropHandItemsEvent args)
    {
        var direction = EntityManager.TryGetComponent(uid, out PhysicsComponent? comp) ? comp.LinearVelocity / 50 : Vector2.Zero;
        var dropAngle = _random.NextFloat(0.8f, 1.2f);

        if (!TryComp(uid, out HandsComponent? handsComp))
            return;

        var worldRotation = _transform.GetWorldRotation(uid).ToVec();
        foreach (var hand in handsComp.Hands.Values)
        {
            if (hand.HeldEntity is not { } held)
                continue;
            if (!_handsSystem.TryDrop(uid, hand, checkActionBlocker: false, handsComp: handsComp))
                continue;

            _throwingSystem.TryThrow(
                held,
                _random.NextAngle().RotateVec(direction / dropAngle + worldRotation / 50),
                0.5f * dropAngle * _random.NextFloat(-0.9f, 1.1f),
                uid,
                0);
        }
    }
}
