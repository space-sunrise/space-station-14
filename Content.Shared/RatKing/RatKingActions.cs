using Content.Shared.Actions;

namespace Content.Shared.RatKing;

public sealed partial class RatKingRaiseArmyActionEvent : InstantActionEvent
{

}

// Sunrise-Start
public sealed partial class RatKingRaiseGuardActionEvent : InstantActionEvent
{

}
// Sunrise-End

public sealed partial class RatKingDomainActionEvent : InstantActionEvent
{

}

public sealed partial class RatKingOrderActionEvent : InstantActionEvent
{
    /// <summary>
    /// The type of order being given
    /// </summary>
    [DataField("type")]
    public RatKingOrderType Type;
}
