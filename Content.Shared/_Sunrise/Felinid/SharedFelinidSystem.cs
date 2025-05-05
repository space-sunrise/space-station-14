using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Throwing;
using Robust.Shared.Containers;

namespace Content.Shared._Sunrise.Felinid;

public abstract class SharedFelinidSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FelinidComponent, PickupAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<FelinidComponent, BeingEquippedAttemptEvent>(OnBeingEquippedAttempt);
        SubscribeLocalEvent<FelinidComponent, ContainerIsInsertingAttemptEvent>(OnHandEquippedAttempt);
        SubscribeLocalEvent<FelinidComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<FelinidComponent, ThrowAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<FelinidComponent, InteractionAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<FelinidComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<FelinidComponent, AttackAttemptEvent>(OnAttempt);
    }

    private void OnInteractAttempt(Entity<FelinidComponent> ent, ref InteractionAttemptEvent args)
    {
        if (ent.Comp.InContainer && !HasComp<FelinidContainerComponent>(args.Target))
            args.Cancelled = true;
    }

    private void OnAttempt(EntityUid uid, FelinidComponent component, CancellableEntityEventArgs args)
    {
        if (component.InContainer)
            args.Cancel();
    }

    private void OnPullAttempt(EntityUid uid, FelinidComponent component, PullAttemptEvent args)
    {
        if (component.InContainer)
            args.Cancelled = true;
    }

    private void OnHandEquippedAttempt(EntityUid uid, FelinidComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!HasComp<FelinidComponent>(args.EntityUid))
            return;

        args.Cancel();
    }

    private void OnBeingEquippedAttempt(Entity<FelinidComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (!HasComp<FelinidComponent>(args.EquipTarget))
            return;

        args.Cancel();
    }

    private void OnPickupAttempt(EntityUid uid, FelinidComponent component, PickupAttemptEvent args)
    {
        if (HasComp<FelinidComponent>(args.Item) || component.InContainer)
            args.Cancel();
    }
}
