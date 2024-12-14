namespace Content.Server._Sunrise.BloodCult.HolyWater;

[RegisterComponent]
public sealed partial class BibleWaterConvertComponent : Component
{
    [DataField("convertedId"), ViewVariables(VVAccess.ReadWrite)]
    public string ConvertedId = "Water";

    [DataField("ConvertedToId"), ViewVariables(VVAccess.ReadWrite)]
    public string ConvertedToId = "Holywater";
}
