using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Shared._Sunrise.AnnouncementSpeaker.Events;

[Serializable, NetSerializable]
public struct MultiSpeakerTtsSource(MapCoordinates coordinates, float volumeModifier = 1f, float maxDistance = 30f)
{
    public MapCoordinates Coordinates = coordinates;
    public float VolumeModifier = volumeModifier;
    public float MaxDistance = maxDistance;
}

[Serializable, NetSerializable]
public sealed class PlayMultiSpeakerTTSEvent : EntityEventArgs
{
    public List<MultiSpeakerTtsSource> Speakers { get; }
    public byte[] SoundData { get; }
    public float? VolumeModifier { get; }
    public float? MaxDistance { get; }
    public bool IsRadio { get; }

    public PlayMultiSpeakerTTSEvent(List<MultiSpeakerTtsSource> speakers, byte[] soundData, float? volumeModifier = null, float? maxDistance = null, bool isRadio = false)
    {
        Speakers = speakers;
        SoundData = soundData;
        VolumeModifier = volumeModifier;
        MaxDistance = maxDistance;
        IsRadio = isRadio;
    }
}

