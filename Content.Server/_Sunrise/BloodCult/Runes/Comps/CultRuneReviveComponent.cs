namespace Content.Server._Sunrise.BloodCult.Runes.Comps;

[RegisterComponent]
public sealed partial class CultRuneReviveComponent : Component
{
    public static uint ChargesLeft = 3;
    public static Dictionary<EntityUid, int> RevivesPerCultist = new();
    
    [ViewVariables(VVAccess.ReadWrite), DataField("maxRevivesPerCultist")]
    public static int MaxRevivesPerCultist = 2;

    [ViewVariables(VVAccess.ReadWrite), DataField("rangeTarget")]
    public float RangeTarget = 0.3f;
}
