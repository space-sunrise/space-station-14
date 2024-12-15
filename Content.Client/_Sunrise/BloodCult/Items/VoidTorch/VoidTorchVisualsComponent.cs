namespace Content.Client._Sunrise.BloodCult.Items.VoidTorch;

[RegisterComponent]
public sealed partial class VoidTorchVisualsComponent : Component
{
    [DataField("stateOn")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateOn = "icon-on";

    [DataField("stateOff")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateOff = "icon";
}
