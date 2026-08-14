using Content.Shared._Sunrise.Antags.Vampires.Events;
using Content.Shared._Sunrise.Antags.Vampires.Components.Effects;
using Content.Shared._Sunrise.Antags.Vampires.Components.Classes;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Antags.Vampires.Systems.Classes;

public sealed class SharedDantalionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly SharedVampireActionUseSystem _vampireActions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireDecoyActionEvent>(OnDecoy);
        SubscribeLocalEvent<VampireBloodBondActionEvent>(OnBloodBond);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var invisQuery = EntityQueryEnumerator<ActiveVampireInvisibilityComponent>();
        while (invisQuery.MoveNext(out var uid, out var invis))
        {
            Entity<ActiveVampireInvisibilityComponent> ent = (uid, invis);

            if (now < invis.EndTime)
                continue;

            RemComp<ActiveVampireInvisibilityComponent>(ent);
            RestoreStealth(ent);
        }
    }

    private void OnDecoy(VampireDecoyActionEvent args)
    {
        if (args.Handled)
            return;

        var uid = args.Performer;
        var actionEntity = args.Action.Owner;
        if (!Exists(actionEntity))
            return;

        if (!TryComp<DantalionComponent>(uid, out var dantalion))
            return;

        Entity<DantalionComponent> ent = (uid, dantalion);
        if (!_vampireActions.TryUse(ent, actionEntity))
            return;

        var hadStealth = TryComp<StealthComponent>(ent, out var stealth);
        var previousEnabled = stealth?.Enabled ?? false;
        var previousVisibility = hadStealth ? _stealth.GetVisibility(ent, stealth) : 1f;

        stealth ??= EnsureComp<StealthComponent>(ent);
        _stealth.SetEnabled(ent, true, stealth);
        _stealth.SetVisibility(ent, args.DecoyVisibility, stealth);

        var invisDuration = args.InvisibilityDuration < TimeSpan.Zero ? TimeSpan.Zero : args.InvisibilityDuration;
        if (invisDuration > TimeSpan.Zero)
        {
            var decoyEv = new VampireDecoyActivatedEvent(
                ent,
                args,
                invisDuration,
                hadStealth,
                previousEnabled,
                previousVisibility);
            RaiseLocalEvent(ent, ref decoyEv, true);
        }
        else
        {
            RestoreStealth(ent, hadStealth, previousEnabled, previousVisibility);

            var decoyEv = new VampireDecoyActivatedEvent(
                ent,
                args,
                TimeSpan.Zero,
                hadStealth,
                previousEnabled,
                previousVisibility);
            RaiseLocalEvent(ent, ref decoyEv, true);
        }

        args.Handled = true;
    }

    private void OnBloodBond(VampireBloodBondActionEvent args)
    {
        if (args.Handled)
            return;

        var uid = args.Performer;
        var actionEntity = args.Action.Owner;
        if (!Exists(actionEntity))
            return;

        if (!TryComp<DantalionComponent>(uid, out var dantalion))
            return;

        Entity<DantalionComponent> ent = (uid, dantalion);
        if (ent.Comp.BloodBondActive)
        {
            ent.Comp.BloodBondActive = false;
            Dirty(ent);
            _popup.PopupPredicted(Loc.GetString("vampire-blood-bond-stop"), ent, ent);
        }
        else
        {
            if (!_vampireActions.TryUse(ent, actionEntity))
                return;

            var attempt = new VampireBloodBondStartAttemptEvent(ent);
            RaiseLocalEvent(ent, ref attempt, true);
            if (attempt.Cancelled)
                return;

            ent.Comp.BloodBondActive = true;
            Dirty(ent);
            _popup.PopupPredicted(Loc.GetString("vampire-blood-bond-start"), ent, ent);
            var started = new VampireBloodBondStartedEvent(ent, args);
            RaiseLocalEvent(ent, ref started, true);
        }

        if (_actions.GetAction(actionEntity) is { } action)
            _actions.SetToggled(action.AsNullable(), ent.Comp.BloodBondActive);

        args.Handled = true;

        if (!ent.Comp.BloodBondActive)
        {
            var stopped = new VampireBloodBondStoppedEvent(ent);
            RaiseLocalEvent(ent, ref stopped, true);
        }
    }

    private void RestoreStealth(Entity<ActiveVampireInvisibilityComponent> ent)
        => RestoreStealth(ent, ent.Comp.HadStealthComponent, ent.Comp.PreviousStealthEnabled, ent.Comp.PreviousStealthVisibility);

    private void RestoreStealth(EntityUid uid, bool hadStealthComponent, bool previousStealthEnabled, float previousStealthVisibility)
    {
        if (!TryComp<StealthComponent>(uid, out var stealth))
            return;

        if (!hadStealthComponent)
        {
            RemComp<StealthComponent>(uid);
            return;
        }

        _stealth.SetEnabled(uid, previousStealthEnabled, stealth);
        _stealth.SetVisibility(uid, previousStealthVisibility, stealth);
    }

}
