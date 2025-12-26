using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Lobby;

[Prototype("lobbyParallax")]
public sealed partial class LobbyParallaxPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set; } = default!;

    [DataField("parallax", required: true)]
    public string Parallax = default!;
}
