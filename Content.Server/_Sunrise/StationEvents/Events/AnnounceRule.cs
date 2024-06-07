using System.Threading;
using Content.Server.Chat.Systems;
using Content.Shared.GameTicking.Components;
using Content.Server.StationEvents.Components;
using Robust.Shared.Player;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.StationEvents.Events;

public sealed class AnnounceRule : StationEventSystem<AnnounceRuleComponent>
{
    [Dependency] private readonly EventManagerSystem _event = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    protected override void Started(EntityUid uid, AnnounceRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        Timer.Spawn(component.RoundStartAnnouncementDelay * 1000, () => DispatchAnnouncement(uid, component, gameRule, args), component.TimerCancel);
    }

    protected override void Ended(EntityUid uid, AnnounceRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);
    }

    private void DispatchAnnouncement(EntityUid uid, AnnounceRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        if (component.EnableAnnouncement)
        {
            if (component.AnnouncementText != null)
                _chat.DispatchGlobalAnnouncement(Loc.GetString(component.AnnouncementText), playSound: true, colorOverride: Color.Green);

            if (component.AnnounceAudio != null)
                Audio.PlayGlobal(component.AnnounceAudio, Filter.Broadcast(), true);
        }
        component.TimerCancel = new CancellationToken();
    }
}
