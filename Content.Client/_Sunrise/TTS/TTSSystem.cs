using Content.Shared._Sunrise.AnnouncementSpeaker.Events;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.TTS;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared;
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
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private ISawmill _sawmill = default!;
    private static readonly MemoryContentRoot ContentRoot = new();
    private static readonly ResPath Prefix = ResPath.Root / "TTS";

    private float _volume;
    private float _radioVolume;
    private int _fileIdx;
    private bool _isQueueEnabled;
    private bool _ghostRadioEnabled;
    private readonly Queue<QueuedTts> _ttsQueue = new();
    private (EntityUid Entity, AudioComponent Component)? _currentPlaying;
    private static readonly AudioResource EmptyAudioResource = new();

    public sealed class QueuedTts(byte[] data, TtsType ttsType)
    {
        public byte[] Data = data;
        public TtsType TtsType = ttsType;
    }

    public enum TtsType
    {
        Voice,
        Radio
    }

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _res.AddRoot(Prefix, ContentRoot);
        _cfg.OnValueChanged(SunriseCCVars.TTSVolume, OnTtsVolumeChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.TTSClientEnabled, OnTtsClientOptionChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.TTSClientQueueEnabled, OnTTSQueueOptionChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.TTSRadioGhostEnabled, OnTtsRadioGhostChanged, true);
        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
        SubscribeNetworkEvent<PlayMultiSpeakerTTSEvent>(OnPlayMultiSpeakerTTS);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(SunriseCCVars.TTSVolume, OnTtsVolumeChanged);
        _cfg.UnsubValueChanged(SunriseCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged);
        _cfg.UnsubValueChanged(SunriseCCVars.TTSClientEnabled, OnTtsClientOptionChanged);
        _cfg.UnsubValueChanged(SunriseCCVars.TTSClientQueueEnabled, OnTTSQueueOptionChanged);
        _cfg.UnsubValueChanged(SunriseCCVars.TTSRadioGhostEnabled, OnTtsRadioGhostChanged);

        ContentRoot.Clear();
        _currentPlaying = null;
        _ttsQueue.Clear();
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

    private void OnTTSQueueOptionChanged(bool option)
    {
        _isQueueEnabled = option;
    }

    private void OnTtsClientOptionChanged(bool option)
    {
        RaiseNetworkEvent(new ClientOptionTTSEvent(option));
    }

    private void OnTtsRadioGhostChanged(bool option)
    {
        _ghostRadioEnabled = option;
    }



    private void PlayNextInQueue()
    {
        if (_ttsQueue.Count == 0)
        {
            return;
        }

        var entry = _ttsQueue.Dequeue();

        var volume = 0f;
        switch (entry.TtsType)
        {
            case TtsType.Radio:
                volume = _radioVolume;
                break;
            case TtsType.Voice:
                volume = _volume;
                break;
        }

        var finalParams = AudioParams.Default.WithVolume(SharedAudioSystem.GainToVolume(volume));

        _currentPlaying = PlayTTSBytes(entry.Data, null, finalParams, true);
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        var volume = ev.IsRadio ? _radioVolume : _volume;

        if (volume == 0)
            return;

        if (ev.IsRadio)
        {
            var localEntity = _playerManager.LocalEntity;
            if(!_ghostRadioEnabled && localEntity.HasValue && HasComp<GhostComponent>(localEntity.Value))
                return;

            if (_isQueueEnabled)
            {
                var entry = new QueuedTts(ev.Data, TtsType.Radio);

                _ttsQueue.Enqueue(entry);
                return;
            }
        }

        volume = SharedAudioSystem.GainToVolume(volume * ev.VolumeModifier);

        var audioParams = AudioParams.Default.WithVolume(volume);

        var entity = GetEntity(ev.SourceUid);
        PlayTTSBytes(ev.Data, entity, audioParams);
    }

    private (AudioResource Resource, ResPath FilePath)? AddTtsAudioResource(byte[] data)
    {
        if (data.Length == 0)
            return null;

        var filePath = new ResPath($"{_fileIdx}.ogg");
        try
        {
            ContentRoot.AddOrUpdateFile(filePath, data);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to add or update file: {ex.Message}");
            _fileIdx++;
            return null;
        }
        var res = new AudioResource();
        res.Load(_dependencyCollection, Prefix / filePath);
        _resourceCache.CacheResource(Prefix / filePath, res);
        return (res, filePath);
    }

    private (EntityUid Entity, AudioComponent Component)? PlayTTSResource(AudioResource res, ResPath filePath, EntityUid? sourceUid = null, AudioParams? audioParams = null, bool globally = false)
    {
        var finalParams = audioParams ?? AudioParams.Default;
        (EntityUid Entity, AudioComponent Component)? playing;
        if (globally)
        {
            playing = _audio.PlayGlobal(res.AudioStream, null, finalParams);
        }
        else
        {
            if (sourceUid != null)
            {
                playing = _audio.PlayEntity(res.AudioStream, sourceUid.Value, null, finalParams);
            }
            else
            {
                playing = _audio.PlayGlobal(res.AudioStream, null, finalParams);
            }
        }
        RemoveFileCursed(filePath);
        _fileIdx++;
        return playing;
    }

    private void OnPlayMultiSpeakerTTS(PlayMultiSpeakerTTSEvent ev)
    {
        if (_volume == 0)
            return;

        var volume = SharedAudioSystem.GainToVolume(_volume);
        var audioParams = AudioParams.Default.WithVolume(volume).WithMaxDistance(30f);

        var audioRes = AddTtsAudioResource(ev.SoundData);
        if (audioRes == null)
            return;

        foreach (var uid in ev.Speakers)
        {
            PlayTTSResource(audioRes.Value.Resource, audioRes.Value.FilePath, GetEntity(uid), audioParams);
        }
    }

    private (EntityUid Entity, AudioComponent Component)? PlayTTSBytes(byte[] data, EntityUid? sourceUid = null, AudioParams? audioParams = null, bool globally = false)
    {
        if (data.Length == 0)
            return null;

        // если sourceUid.Value.Id == 0 то значит эта сущность не прогружена на стороне клиента
        if (sourceUid is { Id: 0 } && !globally)
            return null;

        _sawmill.Debug($"Play TTS audio {data.Length} bytes from {sourceUid} entity");

        var audioRes = AddTtsAudioResource(data);
        if (audioRes == null)
            return null;

        return PlayTTSResource(audioRes.Value.Resource, audioRes.Value.FilePath, sourceUid, audioParams, globally);
    }

    private void RemoveFileCursed(ResPath resPath)
    {
        ContentRoot.RemoveFile(resPath);

        // Push old audio out of the cache to save memory. It is cursed, but should work.
        _resourceCache.CacheResource(Prefix / resPath, EmptyAudioResource);
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
