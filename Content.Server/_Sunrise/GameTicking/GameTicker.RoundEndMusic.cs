using System.Diagnostics.CodeAnalysis;
using Content.Server._Sunrise.GameTicking.Events;
using Content.Shared._Sunrise.Audio.Events;
using Content.Shared.CCVar;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.GameTicking.Prototypes;
using Robust.Shared.Audio;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [ViewVariables]
    private string? _roundEndMusicPool;

    private void InitializeSunriseCVars()
    {
        Subs.CVar(_cfg, SunriseCCVars.RoundEndMusicPool, value => _roundEndMusicPool = value, true);
    }

    private void RaiseRoundEndMusicEvent(TimeSpan roundDuration)
    {
        if (!TryResolveRoundEndMusic(roundDuration, out var music))
            return;

        RaiseNetworkEvent(new RoundEndMusicEvent(music));
    }

    private void RaiseRoundLobbyReadyEvent()
    {
        var ev = new RoundLobbyReadyEvent(RoundId);
        RaiseLocalEvent(ref ev);
    }

    private bool TryResolveRoundEndMusic(TimeSpan roundDuration, [NotNullWhen(true)] out SoundSpecifier? sound)
    {
        sound = null;

        var selection = new RoundEndMusicSelectionEvent(RoundId, roundDuration, CurrentPreset?.ID);
        RaiseLocalEvent(ref selection);

        if (selection.Handheld && !selection.Cancelled && selection.Sound != null)
        {
            sound = selection.Sound;
            return true;
        }

        if (TryResolveRoundEndMusicPool(_roundEndMusicPool, out sound))
            return true;

        if (TryResolveRoundEndCollection(_cfg.GetCVar(CCVars.LobbyMusicCollection), out sound))
            return true;

        return false;
    }

    private bool TryResolveRoundEndCollection(string? collectionId, [NotNullWhen(true)] out SoundSpecifier? sound)
    {
        sound = null;

        if (string.IsNullOrWhiteSpace(collectionId))
            return false;

        if (!_prototypeManager.TryIndex<SoundCollectionPrototype>(collectionId, out _))
        {
            Log.Warning($"Invalid round-end music sound collection specified: {collectionId}");
            return false;
        }

        sound = new SoundCollectionSpecifier(collectionId);
        return true;
    }

    private bool TryResolveRoundEndMusicPool(string? prototypeId, [NotNullWhen(true)] out SoundSpecifier? sound)
    {
        sound = null;

        if (string.IsNullOrWhiteSpace(prototypeId))
            return false;

        if (!_prototypeManager.TryIndex<RoundEndMusicPoolPrototype>(prototypeId, out var pool))
        {
            Log.Warning($"Invalid round-end music prototype specified: {prototypeId}");
            return false;
        }

        var weightedTracks = new Dictionary<RoundEndMusicEntry, float>();
        foreach (var track in pool.Tracks)
        {
            if (track.Weight <= 0f)
                continue;

            weightedTracks[track] = track.Weight;
        }

        if (weightedTracks.Count == 0)
            return false;

        var totalWeight = 0f;
        foreach (var weight in weightedTracks.Values)
        {
            totalWeight += weight;
        }

        var roll = _robustRandom.NextFloat() * totalWeight;
        var cumulative = 0f;

        foreach (var (track, weight) in weightedTracks)
        {
            cumulative += weight;
            if (roll > cumulative)
                continue;

            sound = track.Sound;
            return true;
        }

        return false;
    }
}
