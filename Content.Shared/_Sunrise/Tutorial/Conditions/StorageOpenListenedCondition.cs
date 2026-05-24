namespace Content.Shared._Sunrise.Tutorial.Conditions;

/// <summary>
/// Checks if the player has opened a physical storage container (locker, crate, etc.).
/// For bag/backpack BUI opens use <see cref="BuiOpenListenedCondition"/> instead.
/// Supports any storage or a specific prototype via <see cref="EventListenedConditionBase{T}.Target"/>.
/// </summary>
public sealed partial class StorageOpenListenedCondition : EventListenedConditionBase<StorageOpenListenedCondition>
{
    public override bool ObserveAnyWithoutTarget => true;
}
