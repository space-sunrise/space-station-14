using Robust.Shared.Random;
using System.Linq;
using System.Numerics;
using Content.Server.GameTicking.Prototypes;
using Content.Shared.GameTicking;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [ViewVariables]
    public string? LobbyBackground { get; private set; }

    [ViewVariables]
    private List<ResPath>? _lobbyBackgrounds;

    private static readonly string[] WhitelistedBackgroundExtensions = new string[] {"png", "jpg", "jpeg", "webp"};

    // Sunrise-Start
    [ViewVariables]
    public string? LobbyParalax { get; private set; }

    [ViewVariables]
    private readonly List<string> _lobbyParalaxes =
    [
        "AspidParallax",
        "LighthouseStation",
        "AngleStation",
        "FastSpace",
        "Default",
        "BagelStation",
        "KettleStation",
        "AvriteStation",
        "DeltaStation",
        "TortugaStation",
        "ShipwreckedTurbulence1",
        "PebbleStation",
        "OutpostStation",
        "TrainStation",
        "CoreStation",
        // Яркие паралаксы, выглядят прикольно но кому-то мешают.
        //"Grass",
        //"SillyIsland",
        //"PilgrimAiur"
    ];

    [ViewVariables] private LobbyImage? LobbyImage { get; set; }

    [ViewVariables]
    private readonly List<LobbyImage> _lobbyImages = new ()
    {
        new LobbyImage{Path = "Mobs/Demons/ratvar.rsi", State = "ratvar", Scale = new Vector2(1.15f, 1.15f)},
        new LobbyImage{Path = "_Sunrise/Mobs/Aliens/96x96megafauna.rsi", State = "bubblegum", Scale = new Vector2(4f, 4f)},
        new LobbyImage{Path = "_Sunrise/Mobs/Aliens/96x96megafauna.rsi", State = "mega_legion", Scale = new Vector2(4f, 4f)},
        new LobbyImage{Path = "_Sunrise/Mobs/Aliens/hulk.rsi", State = "Champion_of_Honk", Scale = new Vector2(6f, 6f)},
        new LobbyImage{Path = "_Sunrise/Mobs/Aliens/narsie.rsi", State = "kha'rin", Scale = new Vector2(1f, 1f)},
        new LobbyImage{Path = "_Sunrise/Mobs/Aliens/narsie.rsi", State = "narbee", Scale = new Vector2(1f, 1f)},
        new LobbyImage{Path = "_Sunrise/Mobs/Aliens/narsie.rsi", State = "narsie", Scale = new Vector2(1f, 1f)},
        new LobbyImage{Path = "_Sunrise/Mobs/Aliens/narsie.rsi", State = "reaper", Scale = new Vector2(1f, 1f)},
        new LobbyImage{Path = "_Sunrise/Mobs/Aliens/narsie.rsi", State = "legion", Scale = new Vector2(1f, 1f)},
        new LobbyImage{Path = "_Sunrise/Mobs/Aliens/narsie.rsi", State = "narsie-chains", Scale = new Vector2(1f, 1f)}
    };

    private void InitializeLobbyBackground()
    {
        _lobbyBackgrounds = _prototypeManager.EnumeratePrototypes<LobbyBackgroundPrototype>()
            .Select(x => x.Background)
            .Where(x => WhitelistedBackgroundExtensions.Contains(x.Extension))
            .ToList();


        RandomizeLobbyBackground();
        RandomizeLobbyParalax();
        RandomizeLobbyImage();
    }

    private void RandomizeLobbyParalax() {
        LobbyParalax = _lobbyParalaxes.Any() ? _robustRandom.Pick(_lobbyParalaxes) : null;
    }

    private void RandomizeLobbyImage() {
        LobbyImage = _lobbyImages.Any() ? _robustRandom.Pick(_lobbyImages) : null;
    }
    // Sunrise-End

    private void RandomizeLobbyBackground() {
        LobbyBackground = _lobbyBackgrounds!.Any() ? _robustRandom.Pick(_lobbyBackgrounds!).ToString() : null;
    }
}
