using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.TTS;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
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

    private float _volume;
    private float _radioVolume;
    private int _fileIdx;
    private float _volumeAnnounce;
    private EntityUid _announcementUid = EntityUid.Invalid;

    private Queue<AnnounceTtsEvent> _announceQueue = new();
    private (EntityUid Entity, AudioComponent Component)? _currentPlaying;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _res.AddRoot(Prefix, _contentRoot);
        _cfg.OnValueChanged(SunriseCCVars.TTSVolume, OnTtsVolumeChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.TTSAnnounceVolume, OnTtsAnnounceVolumeChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.TTSClientEnabled, OnTtsClientOptionChanged, true);
        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
        SubscribeNetworkEvent<AnnounceTtsEvent>(OnAnnounceTTSPlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(SunriseCCVars.TTSVolume, OnTtsVolumeChanged);
        _cfg.UnsubValueChanged(SunriseCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged);
        _cfg.UnsubValueChanged(SunriseCCVars.TTSAnnounceVolume, OnTtsAnnounceVolumeChanged);
        _cfg.UnsubValueChanged(SunriseCCVars.TTSClientEnabled, OnTtsClientOptionChanged);
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

    private void OnTtsRadioVolumeChanged(float volume)
    {
        _radioVolume = volume;
    }

    private void OnTtsAnnounceVolumeChanged(float volume)
    {
        _volumeAnnounce = volume;
    }

    private void OnTtsClientOptionChanged(bool option)
    {
        RaiseNetworkEvent(new ClientOptionTTSEvent(option));
    }

    private void OnAnnounceTTSPlay(AnnounceTtsEvent ev)
    {
        if (_volumeAnnounce == 0)
            return;

        _announceQueue.Enqueue(ev);
    }

    private void PlayNextInQueue()
    {
        if (_announceQueue.Count == 0)
        {
            return;
        }

        var ev = _announceQueue.Dequeue();

        if (_announcementUid == EntityUid.Invalid)
            _announcementUid = Spawn(null);

        var volume = SharedAudioSystem.GainToVolume(_volumeAnnounce);
        var finalParams = AudioParams.Default.WithVolume(volume);

        if (ev.AnnouncementSound != null)
        {
            _currentPlaying = _audio.PlayGlobal(ev.AnnouncementSound, _announcementUid, finalParams.AddVolume(-5f));
        }
        _currentPlaying = PlayTTSBytes(ev.Data, _announcementUid, finalParams, true);
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        var volume = ev.IsRadio ? _radioVolume : _volume;

        if (volume == 0)
            return;

        volume = SharedAudioSystem.GainToVolume(volume * ev.VolumeModifier);

        var audioParams = AudioParams.Default.WithVolume(volume);

        var entity = GetEntity(ev.SourceUid);
        PlayTTSBytes(ev.Data, entity, audioParams);
    }

    private (EntityUid Entity, AudioComponent Component)? PlayTTSBytes(byte[] data, EntityUid? sourceUid = null, AudioParams? audioParams = null, bool globally = false)
    {
        if (data.Length == 0)
            return null;

        // если sourceUid.Value.Id == 0 то значит эта сущность не прогружена на стороне клиента
        if ((sourceUid != null && sourceUid.Value.Id == 0) && !globally)
            return null;

        _sawmill.Debug($"Play TTS audio {data.Length} bytes from {sourceUid} entity");

        var finalParams = audioParams ?? AudioParams.Default;

        var filePath = new ResPath($"{_fileIdx}.ogg");
        _contentRoot.AddOrUpdateFile(filePath, data);

        var res = new AudioResource();
        res.Load(_dependencyCollection, Prefix / filePath);
        _resourceCache.CacheResource(Prefix / filePath, res);

        (EntityUid Entity, AudioComponent Component)? playing;

        if (globally)
        {
            playing = _audio.PlayGlobal(res.AudioStream, finalParams);
        }
        else
        {
            if (sourceUid != null)
            {
                playing = _audio.PlayEntity(res.AudioStream, sourceUid.Value, finalParams);
            }
            else
            {
                playing = _audio.PlayGlobal(res.AudioStream, finalParams);
            }
        }

        _contentRoot.RemoveFile(filePath);

        _fileIdx++;
        return playing;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_currentPlaying.HasValue)
        {
            var (entity, component) = _currentPlaying.Value;

            if (Deleted(entity))
            {
                _currentPlaying = null;
            }
            else
            {
                return;
            }
        }

        PlayNextInQueue();
    }
}
