using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

/// <summary>
/// Checks if the player has taken an item from a vending machine.
/// <list type="bullet">
/// <item><term><see cref="ItemTarget"/></term><description>Match the dispensed item prototype (e.g. <c>DrinkJuiceOrange</c>). Takes priority over <see cref="EventListenedConditionBase{T}.Target"/>.</description></item>
/// <item><term><see cref="EventListenedConditionBase{T}.Target"/></term><description>Match the vending machine prototype (e.g. <c>VendingMachineDinnerware</c>).</description></item>
/// <item><term>Neither set</term><description>Any take from any vending machine satisfies the condition.</description></item>
/// </list>
/// </summary>
public sealed partial class VendingMachineTakeListenedCondition : EventListenedConditionBase<VendingMachineTakeListenedCondition>
{
    public override bool ObserveAnyWithoutTarget => true;

    /// <summary>
    /// The prototype ID of the item that must be dispensed.
    /// When set, the condition is satisfied only when this specific item is taken.
    /// </summary>
    [DataField]
    public EntProtoId? ItemTarget;
}
