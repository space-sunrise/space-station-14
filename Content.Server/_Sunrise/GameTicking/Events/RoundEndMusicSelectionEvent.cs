using Robust.Shared.Audio;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server._Sunrise.GameTicking.Events;

[ByRefEvent]
public record struct RoundEndMusicSelectionEvent(int RoundId, TimeSpan RoundDuration, string? PresetId)
{
    public readonly int RoundId = RoundId;
    public readonly TimeSpan RoundDuration = RoundDuration;
    public readonly string? PresetId = PresetId;

    public SoundSpecifier? Sound;
    public int Priority = int.MinValue;

    public bool Handheld;
    public bool Cancelled;
}
