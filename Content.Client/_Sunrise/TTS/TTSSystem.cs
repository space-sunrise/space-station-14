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
    [Dependency] private readonly SharedAudioSystem _sharedAudio = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IDependencyCollection _dependencyCollection = default!;

    private ISawmill _sawmill = default!;
    private readonly MemoryContentRoot _contentRoot = new();
    private static readonly ResPath Prefix = ResPath.Root / "TTS";

    private float _volume;
    private float _radioVolume;
    private int _fileIdx;
    private float _volumeAnnounce;

    private Queue<QueuedTts> _ttsQueue = new();
    private (EntityUid Entity, AudioComponent Component)? _currentPlaying;

    public sealed class QueuedTts(byte[] data, SoundSpecifier? announcementSound = null)
    {
        public byte[] Data = data;
        public SoundSpecifier? AnnouncementSound = announcementSound;
    }

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

    public void RequestPreviewTts(string voiceId)
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

        var entry = new QueuedTts(ev.Data, ev.AnnouncementSound);

        _ttsQueue.Enqueue(entry);
    }

    private void PlayNextInQueue()
    {
        if (_ttsQueue.Count == 0)
        {
            return;
        }

        var entry = _ttsQueue.Dequeue();

        var volume = SharedAudioSystem.GainToVolume(_volumeAnnounce);
        var finalParams = AudioParams.Default.WithVolume(volume);

        if (entry.AnnouncementSound != null)
        {
            _currentPlaying = _audio.PlayGlobal(_sharedAudio.GetSound(entry.AnnouncementSound), new EntityUid(), finalParams.AddVolume(-5f));
        }
        _currentPlaying = PlayTTSBytes(entry.Data, null, finalParams, true);
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        var volume = ev.IsRadio ? _radioVolume : _volume;

        if (volume == 0)
            return;

        if (ev.IsRadio)
        {
            var entry = new QueuedTts(ev.Data);

            _ttsQueue.Enqueue(entry);
            return;
        }

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
