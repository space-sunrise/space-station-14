using Robust.Shared.GameStates;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Component for the Epsilon Death Squad Lawset event.
/// This prevents the event from being triggered by standard station events.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EpsilonDeathSquadLawsetComponent : Component
{
} 