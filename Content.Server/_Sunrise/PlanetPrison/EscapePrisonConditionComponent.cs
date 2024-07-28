namespace Content.Server._Sunrise.PlanetPrison;

/// <summary>
/// Just requires that the player is not dead, ignores evac and what not.
/// </summary>
[RegisterComponent, Access(typeof(EscapePrisonConditionSystem))]
public sealed partial class EscapePrisonConditionComponent : Component
{
}
