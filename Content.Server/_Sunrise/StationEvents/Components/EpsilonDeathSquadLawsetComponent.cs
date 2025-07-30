using Content.Server._Sunrise.StationEvents.Events;
using Robust.Shared.GameStates;

namespace Content.Server._Sunrise.StationEvents.Components;

/// <summary>
/// Component for the Epsilon Death Squad Lawset event.
/// Stores the target station where the event should affect borgs.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EpsilonDeathSquadLawsetRule))]
public sealed partial class EpsilonDeathSquadLawsetComponent : Component
{
    /// <summary>
    /// The station where the law changes should be applied.
    /// </summary>
    [DataField("targetStation")]
    public EntityUid TargetStation = EntityUid.Invalid;
}
