using Content.Shared.Mech.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.Containers;

#pragma warning disable IDE0130 // Пространство имён соответствует расширяемой vanilla-системе
namespace Content.Shared.Mech.EntitySystems;

public abstract partial class SharedMechSystem
{
    private void OnPilotRemoved(Entity<MechComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container != entity.Comp.PilotSlot || _timing.ApplyingState)
            return;

        RemoveUser(entity, args.Entity);
        RemComp<NoRotateOnMoveComponent>(entity);

        if (!TerminatingOrDeleted(entity))
            UpdateAppearance(entity, entity.Comp);
    }
}
