using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.TTS;

[Serializable, NetSerializable]
public sealed class AnnounceTtsEvent(byte[] data, ResolvedSoundSpecifier? announcementSound)
    : EntityEventArgs
{
    public byte[] Data { get; } = data;
    public ResolvedSoundSpecifier? AnnouncementSound = announcementSound;
}
