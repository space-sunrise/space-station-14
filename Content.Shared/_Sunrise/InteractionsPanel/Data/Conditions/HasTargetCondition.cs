using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class HasTargetCondition : IAppearCondition
{
    [DataField]
    public bool AllowSelfTargeting { get; private set; }

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        if (target == EntityUid.Invalid)
            return false;

        if (!entityManager.EntityExists(target))
            return false;

        if (!AllowSelfTargeting && initiator == target)
            return false;

        return true;
    }
}
