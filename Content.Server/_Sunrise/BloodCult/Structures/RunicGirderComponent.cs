namespace Content.Server._Sunrise.BloodCult.Structures;

[RegisterComponent]
public sealed partial class RunicGirderComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public string DropItemID = "CultRunicMetal1";

    [ViewVariables(VVAccess.ReadOnly)]
    public string UsedItemID = "TrueRitualDagger";
}
