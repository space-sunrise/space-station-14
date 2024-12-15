namespace Content.Client._Sunrise.BloodCult.Structures;

[RegisterComponent]
public sealed partial class CultCraftStructureVisualsComponent : Component
{
    [DataField("stateOn")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateOn = "icon";

    [DataField("stateOff")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateOff = "icon-off";
}
