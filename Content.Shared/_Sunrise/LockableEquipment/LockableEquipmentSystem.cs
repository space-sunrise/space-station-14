using Content.Shared._Sunrise.Equipment.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.LocableEquipment;

public sealed class LockableEquipmentSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<LockableEquipmentComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(EntityUid uid, LockableEquipmentComponent comp, ref ComponentGetState args)
    {
        args.State = new LockableEquipmentComponentState(comp.Slot, comp.Enabled);
    }
}