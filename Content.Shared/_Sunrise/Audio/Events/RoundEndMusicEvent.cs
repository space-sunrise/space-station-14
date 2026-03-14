using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Audio.Events;

[Serializable, NetSerializable]
public sealed class RoundEndMusicEvent(SoundSpecifier music) : EntityEventArgs
{
    public SoundSpecifier Music { get; } = music;
}
