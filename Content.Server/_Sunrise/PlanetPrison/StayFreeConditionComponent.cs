namespace Content.Server._Sunrise.PlanetPrison;

/// <summary>
/// Requires that the player is alive and not restrained (no handcuffs or straightjacket).
/// </summary>
[RegisterComponent, Access(typeof(StayFreeConditionSystem))]
public sealed partial class StayFreeConditionComponent : Component
{
}

