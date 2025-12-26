using Robust.Shared.Random;
using System.Linq;
using Content.Server.GameTicking.Prototypes;
using Content.Shared._Sunrise.Lobby;
using Content.Shared.GameTicking;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    // Sunrise-Start
    [ViewVariables]
    public string? LobbyType { get; private set; }
    [ViewVariables]
    public string? LobbyAnimation { get; private set; }
    [ViewVariables]
    public string? LobbyParallax { get; private set; }
    [ViewVariables]
    public string? LobbyBackground { get; private set; }

    [ViewVariables]
    private List<string>? _lobbyParallaxes;

    [ViewVariables]
    private List<string>? _lobbyArts;

    [ViewVariables]
    private List<string>? _lobbyAnimations;

    private void InitializeLobbyBackground()
    {
        _lobbyArts = _prototypeManager.EnumeratePrototypes<LobbyBackgroundPrototype>()
            .Select(x => x.ID)
            .ToList();

        _lobbyParallaxes = _prototypeManager.EnumeratePrototypes<LobbyParallaxPrototype>()
            .Select(x => x.ID)
            .ToList();

        _lobbyAnimations = _prototypeManager.EnumeratePrototypes<LobbyAnimationPrototype>()
            .Select(x => x.ID)
            .ToList();
        RandomizeLobbyBackgroundArt();
        RandomizeLobbyBackgroundParallax();
        RandomizeLobbyBackgroundAnimation();
        RandomizeLobbyBackgroundType();
    }

    private void RandomizeLobbyBackgroundParallax()
    {
        LobbyParallax = _lobbyParallaxes!.Any() ? _robustRandom.Pick(_lobbyParallaxes!) : null;
    }

    private void RandomizeLobbyBackgroundAnimation()
    {
        LobbyAnimation = _lobbyAnimations!.Any() ? _robustRandom.Pick(_lobbyAnimations!) : null;
    }

    private void RandomizeLobbyBackgroundType()
    {
        var values = (LobbyBackgroundType[])Enum.GetValues(typeof(LobbyBackgroundType));
        LobbyType = values[_robustRandom.Next(values.Length)].ToString();
    }

    private void RandomizeLobbyBackgroundArt()
    {
        LobbyBackground = _lobbyArts!.Any() ? _robustRandom.Pick(_lobbyArts!) : null;
    }
    // Sunrise-End
}
