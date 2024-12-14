using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.BloodCult.Components;

[RegisterComponent]
public sealed partial class CultBuffComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), DataField("buffTime")]
    public TimeSpan BuffTime = TimeSpan.FromSeconds(60);

    public static float NearbyTilesBuffRadius = 1f;

    public static readonly TimeSpan CultTileBuffTime = TimeSpan.FromSeconds(5);

    [DataField]
    public ProtoId<AlertPrototype> BuffAlert = "CultBuffed";
}
