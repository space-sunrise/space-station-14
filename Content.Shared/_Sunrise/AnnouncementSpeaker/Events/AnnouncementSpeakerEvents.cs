using Content.Shared.Station.Components;
using Robust.Shared.Audio;

namespace Content.Shared._Sunrise.AnnouncementSpeaker.Events;

/// <summary>
/// Event raised when a station-wide announcement should be played through speakers.
/// This replaces the global broadcast system with a speaker-based network.
/// </summary>
[ByRefEvent]
public readonly record struct AnnouncementSpeakerEvent(
    EntityUid Station,
    string Message,
    ResolvedSoundSpecifier? AnnouncementSound,
    string? AnnounceVoice,
    byte[]? TtsData = null)
{
}
