using Content.Server.StationEvents.Events;
using Robust.Shared.Audio;
using System.Threading;

namespace Content.Server.StationEvents.Components;

[RegisterComponent]
public sealed partial class AnnounceRuleComponent : Component
{
    [DataField]
    public bool EnableAnnouncement;

    [DataField]
    public SoundSpecifier? AnnounceAudio;

    [DataField("roundStartAnnouncementDelay")]
    public int RoundStartAnnouncementDelay = 60; // 1 minute from roundstart by default

    [DataField("announcementText")]
    public string? AnnouncementText;

    public CancellationToken TimerCancel = new();
}
