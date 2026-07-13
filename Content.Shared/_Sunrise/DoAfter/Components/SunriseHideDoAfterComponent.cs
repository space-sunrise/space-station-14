using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.DoAfter.Components;

/// <summary>
/// Marker that prevents displaying the do-after performer to other players.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SunriseHideDoAfterComponent : Component;
