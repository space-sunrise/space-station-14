using Robust.Shared.Map;

namespace Content.Server._Sunrise.Boss.Components;

/// <summary>
///     This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class HellSpawnFighterComponent : Component
{
    [DataField]
    public EntityCoordinates? TeleportedFromCoordinates;
}
