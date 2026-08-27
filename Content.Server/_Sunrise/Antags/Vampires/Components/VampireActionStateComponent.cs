using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Antags.Vampires.Components;

/// <summary>
/// Выданные вампиру action.
/// </summary>
[RegisterComponent]
public sealed partial class VampireActionStateComponent : Component
{
    /// <summary>
    /// Action по ID прототипа.
    /// </summary>
    public Dictionary<EntProtoId, EntityUid> Actions = [];
}
