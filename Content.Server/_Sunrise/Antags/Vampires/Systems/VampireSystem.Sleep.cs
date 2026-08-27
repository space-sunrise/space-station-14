using Content.Server._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Events;
using Content.Shared.Bed.Sleep;
using Content.Shared.Charges.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Antags.Vampires.Systems;

public sealed partial class VampireSystem
{
    // Усыпление цели.

    [Dependency] private readonly IRobustRandom _random = null!;

    private void InitializeSleep()
    {
        SubscribeLocalEvent<VampireComponent, VampireSleepActionEvent>(OnSleep);
        SubscribeLocalEvent<VampireComponent, DoAfterAttemptEvent<VampireSleepDoAfterEvent>>(OnSleepDoAfterAttempt);
        SubscribeLocalEvent<VampireComponent, VampireSleepDoAfterEvent>(OnSleepDoAfter);
    }

    private void OnSleep(Entity<VampireComponent> ent, ref VampireSleepActionEvent args)
    {
        if (args.Handled ||
            !Exists(args.Target) ||
            !TryComp<VampireConfigurationComponent>(ent, out var configuration))
        {
            return;
        }

        if (!TryGetPowerLevelPrototype(ent.Comp.PowerLevel, out var level))
            return;

        var settings = level.Sleep;
        var target = args.Target;

        if (target == ent.Owner ||
            !_interaction.InRangeAndAccessible(ent.Owner, target, settings.TargetRange))
        {
            return;
        }

        if (IsProtectedByFaith(target) && !settings.IgnoresFaith)
        {
            _popup.PopupEntity(
                Loc.GetString("vampire-target-protected-by-faith"),
                ent.Owner,
                ent.Owner,
                PopupType.MediumCaution);
            return;
        }

        if (HasFlashProtection(target))
        {
            _popup.PopupEntity(
                Loc.GetString("vampire-sleep-protected"),
                ent.Owner,
                ent.Owner,
                PopupType.MediumCaution);
            return;
        }

        if (HasComp<MindShieldComponent>(target))
        {
            _popup.PopupEntity(
                Loc.GetString("vampire-sleep-shielded"),
                ent.Owner,
                ent.Owner,
                PopupType.SmallCaution);
            return;
        }

        if (!CanSpendBlood(ent, settings.BloodCost))
            return;

        var doAfterEvent = new VampireSleepDoAfterEvent
        {
            Victim = GetNetEntity(target),
            Action = GetNetEntity(args.Action.Owner),
            MaxDistance = settings.BreakRange,
            BloodCost = settings.BloodCost,
            Duration = settings.Duration,
            IgnoresFaith = settings.IgnoresFaith,
        };
        var doAfter = new DoAfterArgs(EntityManager, ent.Owner, settings.ChannelTime, doAfterEvent, ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            MovementThreshold = configuration.SleepMovementThreshold,
            RequireCanInteract = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
            Hidden = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(
            Loc.GetString(_random.Pick(configuration.SleepTargetMessages)),
            target,
            target,
            PopupType.SmallCaution);
        args.Handled = true;
    }

    private void OnSleepDoAfterAttempt(
        Entity<VampireComponent> ent,
        ref DoAfterAttemptEvent<VampireSleepDoAfterEvent> args)
    {
        var target = GetEntity(args.Event.Victim);
        if (!Exists(target) || !_interaction.InRangeAndAccessible(ent.Owner, target, args.Event.MaxDistance))
            args.Cancel();
    }

    private void OnSleepDoAfter(Entity<VampireComponent> ent, ref VampireSleepDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            RefundSleepAction(args.Action);
            args.Handled = true;
            return;
        }

        var target = GetEntity(args.Victim);
        if (!Exists(target))
        {
            RefundSleepAction(args.Action);
            args.Handled = true;
            return;
        }

        if (IsProtectedByFaith(target) && !args.IgnoresFaith)
        {
            _popup.PopupEntity(
                Loc.GetString("vampire-target-protected-by-faith"),
                ent.Owner,
                ent.Owner,
                PopupType.MediumCaution);
            RefundSleepAction(args.Action);
            args.Handled = true;
            return;
        }

        if (HasFlashProtection(target))
        {
            _popup.PopupEntity(
                Loc.GetString("vampire-sleep-protected"),
                ent.Owner,
                ent.Owner,
                PopupType.MediumCaution);
            RefundSleepAction(args.Action);
            args.Handled = true;
            return;
        }

        if (HasComp<MindShieldComponent>(target))
        {
            _popup.PopupEntity(
                Loc.GetString("vampire-sleep-shielded"),
                ent.Owner,
                ent.Owner,
                PopupType.SmallCaution);
            RefundSleepAction(args.Action);
            args.Handled = true;
            return;
        }

        if (!CheckAndConsumeBloodCost(ent, bloodCost: args.BloodCost))
        {
            RefundSleepAction(args.Action);
            args.Handled = true;
            return;
        }

        _statusEffects.TryAddStatusEffectDuration(target, SleepingSystem.StatusEffectForcedSleeping, args.Duration);
        args.Handled = true;
    }

    private void RefundSleepAction(NetEntity netAction)
    {
        var action = GetEntity(netAction);
        if (!Exists(action) || !TryComp<LimitedChargesComponent>(action, out var charges))
            return;

        TryComp<AutoRechargeComponent>(action, out var recharge);
        _charges.AddCharges((action, charges, recharge), 1);
        _actions.ClearCooldown(action);
    }
}
