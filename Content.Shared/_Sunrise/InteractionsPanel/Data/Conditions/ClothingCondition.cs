using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ClothingCondition : IAppearCondition
{
    [DataField]
    public bool CheckInitiator { get; private set; }

    [DataField]
    public bool CheckTarget { get; private set; } = true;

    [DataField]
    public bool RequiresClothing { get; private set; }

    /// <summary>
    /// Available slots: head, eyes, ears, mask, outerClothing, jumpsuit, neck, back, belt,
    /// gloves, shoes, pants, socks, bra, id, pocket1, pocket2, suitstorage
    /// </summary>
    [DataField]
    public List<string> SpecificContainers { get; private set; } = new();

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        if (CheckInitiator)
        {
            if (!CheckClothingStatus(initiator, entityManager))
                return false;
        }

        if (CheckTarget)
        {
            if (!CheckClothingStatus(target, entityManager))
                return false;
        }

        return true;
    }

    private bool CheckClothingStatus(EntityUid entity, EntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<ContainerManagerComponent>(entity, out var containerManager))
            return !RequiresClothing;

        var hasClothingInContainers = false;

        foreach (var container in containerManager.Containers)
        {
            if (SpecificContainers.Count > 0 && !SpecificContainers.Contains(container.Key))
                continue;

            if (container.Value.ContainedEntities.Count > 0)
            {
                hasClothingInContainers = true;
                break;
            }
        }

        return RequiresClothing ? hasClothingInContainers : !hasClothingInContainers;
    }
}
