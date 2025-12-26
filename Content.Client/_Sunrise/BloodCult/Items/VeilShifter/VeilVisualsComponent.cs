namespace Content.Client._Sunrise.BloodCult.Items.VeilShifter;

[RegisterComponent]
public sealed partial class VeilVisualsComponent : Component
{
    [DataField("stateOff")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateOff = "icon";

    [DataField("stateOn")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateOn = "icon-on";
}
