using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.TTS;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public enum TTSPlaybackGroup : byte
{
    None,
    Tutorial,
}
