using Content.Shared.Interaction;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.LockableEquipment;

public sealed class LockableEquipmentSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<LockableEquipmentComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<LockableEquipmentComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<LockableEquipmentComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnGetState(Entity<LockableEquipmentComponent> ent, ref ComponentGetState args)
    {
        args.State = new LockableEquipmentComponentState(ent.Comp.Locked, ent.Comp.LockId);
    }

    private void OnHandleState(Entity<LockableEquipmentComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not LockableEquipmentComponentState state)
            return;        
        ent.Comp.Locked = state.Locked;
        ent.Comp.LockId = state.LockId;
    }

    private void OnInteractUsing(Entity<LockableEquipmentComponent> ent, ref InteractUsingEvent args)
    {
        RaiseLocalEvent(ent.Owner, new UseKeyOnLockEvent(args.User, args.Used));
    }
}
