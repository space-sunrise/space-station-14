using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Boss.Components;

/// <summary>
///     Компонент, дающий сущности абилку выпускать тентакли из-под земли
/// </summary>
[RegisterComponent]
public sealed partial class HellSpawnTentacleComponent : Component
{
    [DataField]
    public EntProtoId? LeftGrabAction = "ActionHellSpawnTentacleLeft";
    [DataField]
    public EntProtoId? RightGrabAction = "ActionHellSpawnTentacleRight";

    [DataField]
    public EntityUid? LeftGrabActionEntity;
    [DataField]
    public EntityUid? RightGrabActionEntity;
}
