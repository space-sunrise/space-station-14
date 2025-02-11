using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Robust.Shared.Containers;

namespace Content.Shared._Sunrise.Felinid;

public sealed class SharedFelinidSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FelinidComponent, PickupAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<FelinidComponent, BeingEquippedAttemptEvent>(OnBeingEquippedAttempt);
        SubscribeLocalEvent<FelinidComponent, ContainerIsInsertingAttemptEvent>(OnHandEquippedAttempt);

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
        if (!HasComp<FelinidComponent>(args.Item))
            return;

        args.Cancel();
    }
}
