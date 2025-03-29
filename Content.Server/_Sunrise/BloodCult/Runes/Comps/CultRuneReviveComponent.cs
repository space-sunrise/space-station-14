namespace Content.Server._Sunrise.BloodCult.Runes.Comps;

[RegisterComponent]
public sealed partial class CultRuneReviveComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("rangeTarget")]
    public float RangeTarget = 0.3f;
}
