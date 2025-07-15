namespace Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;

public interface IInteractionCondition
{
    bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager);
}

public interface IAppearCondition : IInteractionCondition
{
}
