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
    }

    private void OnGetState(Entity<LockableEquipmentComponent> ent, ref ComponentGetState args)
    {
        args.State = new LockableEquipmentComponentState(ent.Comp.Locked, ent.Comp.LockId);
    }
}
