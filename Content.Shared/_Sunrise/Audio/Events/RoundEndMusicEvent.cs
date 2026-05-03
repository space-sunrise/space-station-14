using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Audio.Events;

[Serializable, NetSerializable]
public readonly record struct RoundEndMusicTrack(SoundSpecifier Music, float Weight = 1f);

[Serializable, NetSerializable]
public sealed class RoundEndMusicEvent(List<RoundEndMusicTrack> tracks) : EntityEventArgs
{
    public List<RoundEndMusicTrack> Tracks { get; } = tracks;
}
