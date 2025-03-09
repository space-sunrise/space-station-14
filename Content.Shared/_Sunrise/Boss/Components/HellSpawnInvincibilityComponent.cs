using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Boss.Components;

[RegisterComponent]
public sealed partial class HellSpawnInvincibilityComponent : Component
{
    [DataField]
    public float? BaseSprintSpeed;

    [DataField]
    public EntProtoId? InvincibilityAction = "ActionHellSpawnInvincibility";

    [DataField]
    public EntityUid? InvincibilityActionEntity;
}
