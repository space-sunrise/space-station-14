using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Boss.Components;

[RegisterComponent]
public sealed partial class HellSpawnSpiralComponent : Component
{
    [DataField]
    public int FireballCount = 4;

    [DataField]
    public EntProtoId? SpiralAction = "ActionHellSpawnSpiral";

    [DataField] public EntityUid? SpiralActionEntity;
}
