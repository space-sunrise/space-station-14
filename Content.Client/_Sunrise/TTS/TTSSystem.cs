using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.TTS;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IDependencyCollection _dependencyCollection = default!;

    private ISawmill _sawmill = default!;
    private readonly MemoryContentRoot _contentRoot = new();
    private static readonly ResPath Prefix = ResPath.Root / "TTS";
    private static readonly AudioResource EmptyAudioResource = new();

    private const float TTSVolume = 0f;
    private const float AnnounceVolume = 0f;

    private float _volume;
    private float _radioVolume;
    private int _fileIdx;
    private float _volumeAnnounce;
    private EntityUid _announcementUid = EntityUid.Invalid;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _res.AddRoot(Prefix, _contentRoot);
        _cfg.OnValueChanged(SunriseCCVars.TTSVolume, OnTtsVolumeChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.TTSAnnounceVolume, OnTtsAnnounceVolumeChanged, true);
        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
        SubscribeNetworkEvent<AnnounceTtsEvent>(OnAnnounceTTSPlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(SunriseCCVars.TTSVolume, OnTtsVolumeChanged);
        _cfg.UnsubValueChanged(SunriseCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged);
        _cfg.UnsubValueChanged(SunriseCCVars.TTSAnnounceVolume, OnTtsAnnounceVolumeChanged);
        _contentRoot.Dispose();
    }

    public void RequestPreviewTTS(string voiceId)
    {
        RaiseNetworkEvent(new RequestPreviewTTSEvent(voiceId));
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnTtsAnnounceVolumeChanged(float volume)
    {
        _volumeAnnounce = volume;
    }

    private void OnAnnounceTTSPlay(AnnounceTtsEvent ev)
    {
        if (_volumeAnnounce == 0)
            return;

        if (_announcementUid == EntityUid.Invalid)
            _announcementUid = Spawn(null);

        var finalParams = new AudioParams() {Volume = AnnounceVolume + SharedAudioSystem.GainToVolume(_volumeAnnounce)};

        PlayTTSBytes(ev.Data, _announcementUid, finalParams, true);
    }

    private void OnTtsRadioVolumeChanged(float volume)
    {
        _radioVolume = volume;
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        var volume = ev.IsRadio ? _radioVolume : _volume;

        if (volume == 0)
            return;

        volume = TTSVolume + SharedAudioSystem.GainToVolume(volume * ev.VolumeModifier);

        var audioParams = AudioParams.Default.WithVolume(volume);

        PlayTTSBytes(ev.Data, GetEntity(ev.SourceUid), audioParams);
    }

    private void PlayTTSBytes(byte[] data, EntityUid? sourceUid = null, AudioParams? audioParams = null, bool globally = false)
    {
        _sawmill.Debug($"Play TTS audio {data.Length} bytes from {sourceUid} entity");
        if (data.Length == 0)
            return;

        var finalParams = audioParams ?? AudioParams.Default;

        var filePath = new ResPath($"{_fileIdx}.ogg");
        _contentRoot.AddOrUpdateFile(filePath, data);

        var res = new AudioResource();
        res.Load(_dependencyCollection, Prefix / filePath);
        _resourceCache.CacheResource(Prefix / filePath, res);

        if (sourceUid != null)
        {
            _audio.PlayEntity(res.AudioStream, sourceUid.Value, finalParams);
        }
        else
        {
            _audio.PlayGlobal(res.AudioStream, finalParams);
        }

        if (globally)
            _audio.PlayGlobal(res.AudioStream, finalParams);
        else
        {
            if (sourceUid == null)
                _audio.PlayGlobal(res.AudioStream, finalParams);
            else
                _audio.PlayEntity(res.AudioStream, sourceUid.Value, finalParams);
        }

        _contentRoot.RemoveFile(filePath);

        _fileIdx++;
    }
}
