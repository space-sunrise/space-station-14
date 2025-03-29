using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.BloodCult.TimedProduction;

[RegisterComponent]
public sealed partial class CultistFactoryComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Active = true;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("cooldown")]
    public int Cooldown = 240;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? NextTimeUse;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("products")]
    public Dictionary<string, List<EntProtoId>> Products;
}
