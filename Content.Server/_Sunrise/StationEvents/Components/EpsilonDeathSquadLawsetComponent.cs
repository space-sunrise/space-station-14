using Content.Server._Sunrise.StationEvents.Events;

namespace Content.Server._Sunrise.StationEvents.Components;

/// <summary>
/// Component for the Epsilon Death Squad Lawset event.
/// Stores the target station where the event should affect borgs.
/// </summary>
[RegisterComponent, Access(typeof(EpsilonDeathSquadLawsetRule))]
public sealed partial class EpsilonDeathSquadLawsetComponent : Component
{
}
