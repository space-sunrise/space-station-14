using Robust.Shared.Random;
using System.Linq;
using Content.Shared._Sunrise.Lobby;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    #region Public variables

    [ViewVariables]
    public string? LobbyType { get; private set; }

    [ViewVariables]
    public ProtoId<LobbyAnimationPrototype>? LobbyAnimation { get; private set; }

    [ViewVariables]
    public ProtoId<LobbyParallaxPrototype>? LobbyParallax { get; private set; }

    [ViewVariables]
    public ProtoId<LobbyArtPrototype>? LobbyArt { get; private set; }

    [ViewVariables]
    public ProtoId<LobbyBackgroundPresetPrototype> LobbyBackgroundPreset { get; private set; }

    #endregion

    #region Private variables

    [ViewVariables]
    private List<string>? _lobbyArts;

    [ViewVariables]
    private List<string>? _lobbyAnimations;

    [ViewVariables]
    private List<string>? _lobbyParallaxes;

    #endregion

    private void InitializeLobbyBackground()
    {
        Subs.CVar(_cfg, SunriseCCVars.LobbyBackgroundPreset, x => LobbyBackgroundPreset = x, true);
        var preset = _prototypeManager.Index(LobbyBackgroundPreset);

        _lobbyArts = _prototypeManager.EnumeratePrototypes<LobbyArtPrototype>()
            .Select(x => x.ID)
            .Where(x => preset.AllArtsAllowed || preset.WhitelistArts.Contains(x))
            .ToList();

        _lobbyAnimations = _prototypeManager.EnumeratePrototypes<LobbyAnimationPrototype>()
            .Select(x => x.ID)
            .Where(x => preset.AllAnimationsAllowed || preset.WhitelistAnimations.Contains(x))
            .ToList();

        _lobbyParallaxes = _prototypeManager.EnumeratePrototypes<LobbyParallaxPrototype>()
            .Select(x => x.ID)
            .Where(x => preset.AllParallaxesAllowed || preset.WhitelistParallaxes.Contains(x))
            .ToList();

        RandomizeLobbyBackgroundArt();
        RandomizeLobbyBackgroundParallax();
        RandomizeLobbyBackgroundAnimation();
        RandomizeLobbyBackgroundType();
    }

    #region Randomize methods

    private void RandomizeLobbyBackgroundArt()
    {
        if (_lobbyArts is null || _lobbyArts.Count == 0)
            return;

        LobbyArt = _robustRandom.Pick(_lobbyArts);
    }

    private void RandomizeLobbyBackgroundAnimation()
    {
        if (_lobbyAnimations is null || _lobbyAnimations.Count == 0)
            return;

        LobbyAnimation = _robustRandom.Pick(_lobbyAnimations);
    }

    private void RandomizeLobbyBackgroundParallax()
    {
        if (_lobbyParallaxes is null || _lobbyParallaxes.Count == 0)
            return;

        LobbyParallax = _robustRandom.Pick(_lobbyParallaxes);
    }

    private void RandomizeLobbyBackgroundType()
    {
        var values = Enum.GetValues<LobbyBackgroundType>();
        LobbyType = values[_robustRandom.Next(values.Length)].ToString();
    }

    #endregion
}
