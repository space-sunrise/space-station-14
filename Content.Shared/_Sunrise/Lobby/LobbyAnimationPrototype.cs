using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Lobby;

[Prototype("lobbyAnimation")]
public sealed partial class LobbyAnimationPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; set; } = default!;

    [DataField("rawPath", required: true)]
    public string RawPath = default!;

    [DataField("scale")]
    public Vector2 Scale = new(1f, 1f);

    [DataField("state")]
    public string State = "animation";
}
