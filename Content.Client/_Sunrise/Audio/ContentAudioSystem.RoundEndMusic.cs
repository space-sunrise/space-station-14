using Content.Shared._Sunrise.Audio.Events;
using Content.Shared.CCVar;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Client.Audio;

public sealed partial class ContentAudioSystem
{
    private EntityUid? _roundEndAudioStream;

    private void OnRoundEndMusic(RoundEndMusicEvent ev)
    {
        EndLobbyMusic();
        _lobbyPlaylist = null;
        StopRoundEndMusic();

        if (!_configManager.GetCVar(SunriseCCVars.RoundEndMusicEnabled))
            return;

        _roundEndAudioStream = _audio.PlayGlobal(
            ev.Music,
            Filter.Local(),
            false,
            ev.Music.Params.AddVolume(SharedAudioSystem.GainToVolume(_configManager.GetCVar(CCVars.LobbyMusicVolume))))
            ?.Entity;
    }

    private void StopRoundEndMusic()
    {
        _roundEndAudioStream = _audio.Stop(_roundEndAudioStream);
    }
}
