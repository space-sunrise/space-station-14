namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Component that marks a beaker as a cryostasis beaker, which prevents solutions inside from being heated.
/// Solutions in cryostasis beakers will remain at or below room temperature (293.15K).
/// </summary>
[RegisterComponent]
public sealed partial class CryostasisBeakerComponent : Component
{
    /// <summary>
    /// Maximum temperature that solutions in this beaker can reach.
    /// Default is room temperature (293.15K).
    /// </summary>
    [DataField("maxTemperature")]
    public float MaxTemperature = 293.15f;
}