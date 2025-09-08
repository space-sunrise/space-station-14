using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.AnnouncementSpeaker.Events;

[Serializable, NetSerializable]
public sealed class PlayMultiSpeakerTTSEvent : EntityEventArgs
{
    public List<NetEntity> Speakers { get; }
    public byte[] SoundData { get; }
    public float? Volume { get; }
    public float? MaxDistance { get; }
    public bool IsRadio { get; }

    public PlayMultiSpeakerTTSEvent(List<NetEntity> speakers, byte[] soundData, float? volume = null, float? maxDistance = null, bool isRadio = false)
    {
        Speakers = speakers;
        SoundData = soundData;
        Volume = volume;
        MaxDistance = maxDistance;
        IsRadio = isRadio;
    }
}

