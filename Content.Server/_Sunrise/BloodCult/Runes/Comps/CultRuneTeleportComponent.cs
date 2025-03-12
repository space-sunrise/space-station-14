namespace Content.Server._Sunrise.BloodCult.Runes.Comps;

[RegisterComponent]
public sealed partial class CultRuneTeleportComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("label")]
    public string? Label;

    [ViewVariables(VVAccess.ReadWrite), DataField("rangeTarget")]
    public float RangeTarget = 0.3f;
}
