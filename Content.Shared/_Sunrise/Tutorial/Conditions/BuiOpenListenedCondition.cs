namespace Content.Shared._Sunrise.Tutorial.Conditions;

/// <summary>
/// Checks if the player has opened any bound user interface on an observable entity.
/// Supports any entity or a specific prototype via <see cref="EventListenedConditionBase{T}.Target"/>.
/// For physical storage containers use <see cref="StorageOpenListenedCondition"/> instead.
/// </summary>
public sealed partial class BuiOpenListenedCondition : EventListenedConditionBase<BuiOpenListenedCondition>
{
    public override bool ObserveAnyWithoutTarget => true;
}
