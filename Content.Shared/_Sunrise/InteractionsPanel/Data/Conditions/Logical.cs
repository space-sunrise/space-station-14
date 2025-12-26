using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class InteractionAndCondition : IAppearCondition
{
    [DataField]
    public List<IInteractionCondition> Conditions { get; private set; } = new();

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        foreach (var condition in Conditions)
        {
            if (!condition.IsMet(initiator, target, entityManager))
                return false;
        }
        return true;
    }
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class InteractionOrCondition : IAppearCondition
{
    [DataField]
    public List<IInteractionCondition> Conditions { get; private set; } = new();

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        if (Conditions.Count == 0)
            return true;

        foreach (var condition in Conditions)
        {
            if (condition.IsMet(initiator, target, entityManager))
                return true;
        }
        return false;
    }
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class InteractionNotCondition : IAppearCondition
{
    [DataField]
    public IInteractionCondition Condition { get; private set; } = default!;

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        return !Condition.IsMet(initiator, target, entityManager);
    }
}
