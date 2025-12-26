namespace Content.Shared.EnergyDome;

/// <summary>
/// marker component that allows linking the dome generator with the dome itself
/// </summary>

[RegisterComponent]
public sealed partial class EnergyDomeComponent : Component
{
    /// <summary>
    /// A linked generator that uses energy
    /// </summary>
    [DataField]
    public EntityUid? Generator;
}
