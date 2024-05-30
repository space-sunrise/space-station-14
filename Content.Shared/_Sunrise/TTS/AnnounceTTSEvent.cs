using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.TTS;

[Serializable, NetSerializable]
public sealed class AnnounceTtsEvent(byte[] data)
    : EntityEventArgs
{
    public byte[] Data { get; } = data;
}
