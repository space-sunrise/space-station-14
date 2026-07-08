using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;

[RegisterComponent]
public sealed partial class PlayerJoinableMapComponent : Component
{
    /// <summary>
    /// Player-joinable map configuration used by this station.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PlayerJoinableMapPrototype> Map;
}
