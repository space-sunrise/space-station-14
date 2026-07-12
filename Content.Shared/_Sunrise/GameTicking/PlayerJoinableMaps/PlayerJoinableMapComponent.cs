using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;

/// <summary>
/// Marks a station entity as the join target for a separately loaded player-accessible map.
/// </summary>
/// <remarks>
/// Add this component to the station prototype declared by the external map's
/// <see cref='Content.Shared.Maps.GameMapPrototype'/>, not to the main station that triggers loading.
/// </remarks>
[RegisterComponent]
public sealed partial class PlayerJoinableMapComponent : Component
{
    /// <summary>
    /// Player-joinable map configuration used by this station.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PlayerJoinableMapPrototype> Map;
}
