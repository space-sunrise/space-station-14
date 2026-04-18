using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ItemCondition : IAppearCondition
{
    [DataField]
    public bool CheckInitiator { get; private set; } = true;

    [DataField]
    public bool CheckTarget { get; private set; }

    [DataField]
    public List<EntProtoId> ItemWhiteList { get; private set; } = new();

    [DataField]
    public bool CheckEquipped { get; private set; }

    [DataField]
    public List<string> EquipmentSlots { get; private set; } = new();

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        var handsSystem = entityManager.EntitySysManager.GetEntitySystem<SharedHandsSystem>();

        if (CheckInitiator)
        {
            if (!HasRequiredItem(initiator, entityManager, handsSystem))
                return false;
        }

        if (CheckTarget)
        {
            if (!HasRequiredItem(target, entityManager, handsSystem))
                return false;
        }

        return true;
    }

    private bool HasRequiredItem(EntityUid entity, EntityManager entityManager, SharedHandsSystem handsSystem)
    {
        if (entityManager.TryGetComponent<HandsComponent>(entity, out var handsComponent))
        {
            foreach (var heldEntity in handsSystem.EnumerateHeld((entity, handsComponent)))
            {
                if (IsMatchingItem(heldEntity, entityManager))
                    return true;
            }
        }

        if (CheckEquipped && entityManager.TryGetComponent<ContainerManagerComponent>(entity, out var containerManager))
        {
            foreach (var (key, container) in containerManager.Containers)
            {
                if (EquipmentSlots.Count > 0 && !EquipmentSlots.Contains(key))
                    continue;

                foreach (var containedEntity in container.ContainedEntities)
                {
                    if (IsMatchingItem(containedEntity, entityManager))
                        return true;
                }
            }
        }

        return false;
    }

    private bool IsMatchingItem(EntityUid entity, EntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<MetaDataComponent>(entity, out var meta))
            return false;

        return meta.EntityPrototype != null && ItemWhiteList.Contains(meta.EntityPrototype.ID);
    }
}
