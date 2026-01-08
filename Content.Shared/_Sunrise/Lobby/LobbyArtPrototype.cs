using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Lobby;

[Prototype]
public sealed partial class LobbyArtPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; set; } = default!;

    /// <summary>
    /// The sprite path to use as the background. This should ideally be 1920x1080.
    /// </summary>
    [DataField("background", required: true)]
    public string Background = default!;

    /// <summary>
    /// The title of the background to be displayed in the lobby.
    /// </summary>
    [DataField]
    public LocId Title = "lobby-state-background-unknown-title";

    /// <summary>
    /// The artist who made the art for the background.
    /// </summary>
    [DataField]
    public LocId Artist = "lobby-state-background-unknown-artist";
}

