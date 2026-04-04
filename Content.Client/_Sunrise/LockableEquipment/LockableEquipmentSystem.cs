using Content.Shared._Sunrise.LockableEquipment;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client._Sunrise.LockableEquipment;

public sealed class LockableEquipmentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LockableEquipmentComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(Entity<LockableEquipmentComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not LockableEquipmentComponentState state)
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (state.Locked)
        {
            sprite.LayerSetState(0, "icon_locked");
        }
        else
        {
            sprite.LayerSetState(0, "icon");
        }
    }
}
