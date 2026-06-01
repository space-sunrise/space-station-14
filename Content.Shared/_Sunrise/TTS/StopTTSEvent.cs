using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.TTS;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class StopTTSEvent(TTSPlaybackGroup playbackGroup) : EntityEventArgs
{
    public TTSPlaybackGroup PlaybackGroup { get; } = playbackGroup;
}
