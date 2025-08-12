using Content.Shared._Sunrise.Bed;
using Content.Shared.Actions;
using Content.Shared.Bed.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Bed;

public abstract class SharedBedSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly ActionContainerSystem _actConts = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
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
        //_actionsSystem.RemoveAction(args.Buckle.Owner, bed.Comp.SleepAction);
        //_sleepingSystem.TryWaking(args.Buckle.Owner);
        RemComp<HealOnBuckleHealingComponent>(bed);
    }
}
