using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.LockableEquipment;

public sealed class LockableEquipmentSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
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
        var state = (LockableEquipmentComponentState)args.Current;
        ent.Comp.Locked = state.Locked;
        ent.Comp.LockId = state.LockId;
    }

    private void OnInteractUsing(Entity<LockableEquipmentComponent> ent, ref InteractUsingEvent args)
    {
        RaiseLocalEvent(ent.Owner, new UseKeyOnLockEvent(args.User, args.Used));
    }
}
