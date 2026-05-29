namespace Content.Shared._Sunrise.Temperature;

/// <summary>
/// Changes the body temperature of melee hit targets.
/// </summary>
[RegisterComponent]
public sealed partial class ChangeTemperatureOnMeleeHitComponent : Component
{
    /// <summary>
    /// Heat added to each melee target, in joules.
    /// </summary>
    [DataField]
    public float Heat = -2500f;

    /// <summary>
    /// Whether the heat change ignores target heat resistance.
    /// </summary>
    [DataField]
    public bool IgnoreHeatResistance = true;
}
