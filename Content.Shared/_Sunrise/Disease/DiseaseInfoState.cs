using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Disease;

[Serializable, NetSerializable]
public sealed class DiseaseInfoState : BoundUserInterfaceState
{
    public float BaseInfectChance;
    public float CoughSneezeInfectChance;
    public int Lethal;
    public int Shield;
    public int CurrentInfected;
    public int TotalInfected;

    public DiseaseInfoState(float baseInfectChance, float coughSneezeInfectChance, int lethal, int shield, int currentInfected, int totalInfected)
    {
        BaseInfectChance = baseInfectChance;
        CoughSneezeInfectChance = coughSneezeInfectChance;
        Lethal = lethal;
        Shield = shield;
        CurrentInfected = currentInfected;
        TotalInfected = totalInfected;
    }
}
