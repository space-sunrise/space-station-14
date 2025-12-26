using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class HasMarkingCondition : IAppearCondition
{
    [DataField]
    public bool CheckInitiator { get; private set; }

    [DataField]
    public bool CheckTarget { get; private set; } = true;

    [DataField(required: true)]
    public List<string> MarkingWhitelist { get; private set; } = new();

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entMan)
    {
        if (CheckInitiator && !HasAnyMarking(initiator, entMan))
            return false;

        if (CheckTarget && !HasAnyMarking(target, entMan))
            return false;

        return true;
    }

    private bool HasAnyMarking(EntityUid uid, EntityManager entMan)
    {
        if (!entMan.TryGetComponent<HumanoidAppearanceComponent>(uid, out var appearance))
            return false;

        foreach (var markingList in appearance.MarkingSet.Markings.Values)
        {
            foreach (var marking in markingList)
            {
                if (MarkingWhitelist.Contains(marking.MarkingId))
                    return true;
            }
        }

        return false;
    }
}
