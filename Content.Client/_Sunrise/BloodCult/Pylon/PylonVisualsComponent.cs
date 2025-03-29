namespace Content.Client._Sunrise.BloodCult.Pylon;

[RegisterComponent]
public sealed partial class PylonVisualsComponent : Component
{
    [DataField("stateOff")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateOff = "pylon_off";

    [DataField("stateOn")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateOn = "pylon";
}
