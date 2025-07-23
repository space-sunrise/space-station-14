using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class SelfOnlyCondition : IAppearCondition
{
    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        if (target == EntityUid.Invalid || !entityManager.EntityExists(target))
            return false;

        return initiator == target;
    }
}
