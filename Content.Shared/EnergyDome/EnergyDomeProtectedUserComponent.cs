namespace Content.Shared.EnergyDome;

/// <summary>
/// marker component that allows linking the dome generator with the dome itself
/// </summary>

[RegisterComponent]
public sealed partial class EnergyDomeProtectedUserComponent : Component
{
    [DataField]
    public EntityUid? DomeEntity;
}
