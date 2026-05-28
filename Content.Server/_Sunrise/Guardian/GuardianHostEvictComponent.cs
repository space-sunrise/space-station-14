using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Guardian;

/// <summary>
/// Grants a guardian host the action to sever the guardian link.
/// </summary>
[RegisterComponent]
public sealed partial class GuardianHostEvictComponent : Component
{
    /// <summary>
    /// Action used to evict the hosted guardian.
    /// </summary>
    [DataField]
    public EntProtoId Action = "ActionGuardianEvict";

    /// <summary>
    /// Runtime action entity.
    /// </summary>
    public EntityUid? ActionEntity;
}
