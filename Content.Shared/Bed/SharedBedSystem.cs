using Content.Shared._Sunrise.Bed;
using Content.Shared.Actions;
using Content.Shared.Bed.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Shared.Bed;

public abstract class SharedBedSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly ActionContainerSystem _actConts = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedMetabolizerSystem _metabolizer = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SleepingSystem _sleepingSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Sunrise-Start
        SubscribeLocalEvent<CanSleepOnBuckleComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<CanSleepOnBuckleComponent, StrappedEvent>(OnStrapped);
        // Sunrise-End

        // Sunrise-Edit
        //SubscribeLocalEvent<HealOnBuckleComponent, MapInitEvent>(OnHealMapInit);
        SubscribeLocalEvent<HealOnBuckleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<HealOnBuckleComponent, UnstrappedEvent>(OnUnstrapped);

        SubscribeLocalEvent<StasisBedComponent, StrappedEvent>(OnStasisStrapped);
        SubscribeLocalEvent<StasisBedComponent, UnstrappedEvent>(OnStasisUnstrapped);
        SubscribeLocalEvent<StasisBedComponent, GotEmaggedEvent>(OnStasisEmagged);
        SubscribeLocalEvent<StasisBedComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<StasisBedBuckledComponent, GetMetabolicMultiplierEvent>(OnStasisGetMetabolicMultiplier);
    }

    // Sunrise-Start
    private void OnStrapped(Entity<CanSleepOnBuckleComponent> bed, ref StrappedEvent args)
    {
        var canSleep = EnsureComp<CanSleepComponent>(args.Buckle);
        _actionsSystem.AddAction(args.Buckle.Owner, ref canSleep.SleepAction, SleepingSystem.SleepActionId, args.Buckle.Owner);
    }

    private void OnUnstrapped(Entity<CanSleepOnBuckleComponent> bed, ref UnstrappedEvent args)
    {
        if (!TryComp<CanSleepComponent>(args.Buckle.Owner, out var canSleep))
            return;

        RemComp<CanSleepComponent>(args.Buckle.Owner);
        _actionsSystem.RemoveAction(args.Buckle.Owner, canSleep.SleepAction);
        _sleepingSystem.TryWaking(args.Buckle.Owner);
        if (canSleep.SleepAction != null)
            _actConts.RemoveAction(canSleep.SleepAction.Value);
    }
    // Sunrise-End

    // Sunrise-Edit
    // private void OnHealMapInit(Entity<HealOnBuckleComponent> ent, ref MapInitEvent args)
    // {
    //     _actConts.EnsureAction(ent.Owner, ref ent.Comp.SleepAction, SleepingSystem.SleepActionId);
    //     Dirty(ent);
    // }

    private void OnStrapped(Entity<HealOnBuckleComponent> bed, ref StrappedEvent args)
    {
        EnsureComp<HealOnBuckleHealingComponent>(bed);
        bed.Comp.NextHealTime = Timing.CurTime + TimeSpan.FromSeconds(bed.Comp.HealTime);
    }

    private void OnUnstrapped(Entity<HealOnBuckleComponent> bed, ref UnstrappedEvent args)
    {
        // Sunrise-Edit
        // If the entity being unbuckled is terminating, we shouldn't try to act upon it, as some components may be gone
        //if (!Terminating(args.Buckle.Owner))
        //{
        //    _actionsSystem.RemoveAction(args.Buckle.Owner, bed.Comp.SleepAction);
        //    _sleepingSystem.TryWaking(args.Buckle.Owner);
        //}

        RemComp<HealOnBuckleHealingComponent>(bed);
    }

    private void OnStasisStrapped(Entity<StasisBedComponent> ent, ref StrappedEvent args)
    {
        EnsureComp<StasisBedBuckledComponent>(args.Buckle);
        _metabolizer.UpdateMetabolicMultiplier(args.Buckle);
    }

    private void OnStasisUnstrapped(Entity<StasisBedComponent> ent, ref UnstrappedEvent args)
    {
        RemComp<StasisBedBuckledComponent>(ent);
        _metabolizer.UpdateMetabolicMultiplier(args.Buckle);
    }

    private void OnStasisEmagged(Entity<StasisBedComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        ent.Comp.Multiplier = 1f / ent.Comp.Multiplier;
        UpdateMetabolisms(ent.Owner);
        Dirty(ent);

        args.Handled = true;
    }

    private void OnPowerChanged(Entity<StasisBedComponent> ent, ref PowerChangedEvent args)
    {
        UpdateMetabolisms(ent.Owner);
    }

    private void OnStasisGetMetabolicMultiplier(Entity<StasisBedBuckledComponent> ent, ref GetMetabolicMultiplierEvent args)
    {
        if (!TryComp<BuckleComponent>(ent, out var buckle) || buckle.BuckledTo is not { } buckledTo)
            return;

        if (!TryComp<StasisBedComponent>(buckledTo, out var stasis))
            return;

        if (!_powerReceiver.IsPowered(buckledTo))
            return;

        args.Multiplier *= stasis.Multiplier;
    }

    protected void UpdateMetabolisms(Entity<StrapComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        foreach (var buckledEntity in ent.Comp.BuckledEntities)
        {
            _metabolizer.UpdateMetabolicMultiplier(buckledEntity);
        }
    }
}
