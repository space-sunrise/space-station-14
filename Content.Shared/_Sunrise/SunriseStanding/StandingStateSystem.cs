using Content.Shared._Sunrise.Abilities.Resomi;
using Content.Shared._Sunrise.Jump;
using Content.Shared.ActionBlocker;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Content.Shared.Movement.Events;
using Content.Shared.Nutrition;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Sunrise.SunriseStanding;

public sealed class SunriseStandingStateSystem : EntitySystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private readonly EntProtoId _fallStatusEffectKey = "StatusEffectFall";
    public const float FallModifier = 0.2f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrawlerComponent, KnockedDownEvent>(OnDown);
        SubscribeLocalEvent<FallStatusEffectComponent, StatusEffectRelayedEvent<TileFrictionEvent>>(OnFallTileFriction);
    }
    /// <summary>
    /// All commented code causes severe lag on the client side. This is most likely due to StatusEffectRelayedEvent.
    /// If anyone can fix this, great, but I think it's almost impossible.
    /// </summary>
    // private void OnStatusEffectStartUp(Entity<FallStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    // {
    //     _blocker.UpdateCanMove(args.Target);
    // }
    // private void OnStatusEffectRemoved(Entity<FallStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    // {
    //     _blocker.UpdateCanMove(args.Target);
    // }

    // private void OnMoveAttempt(Entity<FallStatusEffectComponent> ent, ref StatusEffectRelayedEvent<UpdateCanMoveEvent> args)
    // {
    //     args.Args.Cancel();
    // }

    private void OnFallTileFriction(Entity<FallStatusEffectComponent> ent, ref StatusEffectRelayedEvent<TileFrictionEvent> args)
    {
        var parent = Transform(ent).ParentUid;

        if (!TryComp<CanFallComponent>(parent, out var fall))
            return;

        var ev = args.Args;
        ev.Modifier *= fall.Friction;
        args.Args = ev;
    }

    private void OnDown(EntityUid uid, CrawlerComponent comp, KnockedDownEvent ev)
    {
        if (_gravity.IsWeightless(uid))
            return;

        Fall(uid);
    }

    public void Fall(EntityUid uid)
    {
        if (!TryComp<CanFallComponent>(uid, out var fall))
            return;

        if (!TryComp<StaminaComponent>(uid, out var stamina))
            return;

        var threshold = stamina.CritThreshold * (1 - fall.MinimumStamina);

        if (stamina.StaminaDamage >= threshold)
        {
            _popup.PopupPredicted(Loc.GetString("cant-fall-no-stamina"), null, uid, uid);
            return;
        }


        if (!TryComp<PhysicsComponent>(uid, out var physics) || _statusEffects.HasEffectComp<JumpStatusEffectComponent>(uid))
            return;

        var ev = new FallAttemptEvent();
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            return;

        if (!TryComp<KnockedDownComponent>(uid, out var knockedDown))
            return;

        if (knockedDown.AutoStand)
            return;

        var velocity = physics.LinearVelocity;

        if (velocity.LengthSquared() < 0.1f)
            return;

        _physics.SetLinearVelocity(uid, physics.LinearVelocity * fall.VelocityModifier, body: physics);
        _statusEffects.TryAddStatusEffectDuration(uid,
            _fallStatusEffectKey,
            fall.Duration);

        _stamina.TakeStaminaDamage(uid, stamina.CritThreshold * fall.StaminaDamage, null, uid, uid, ignoreResist: true);
    }
}
