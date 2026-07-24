using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

// Sunrise edit start - added helper for CentComm exclusion
using Content.Server._Sunrise.StationEvents;
// Sunrise edit end

namespace Content.Server.StationEvents.Events;

/// <summary>
///     An abstract entity system inherited by all station events for their behavior.
/// </summary>
public abstract class StationEventSystem<T> : GameRuleSystem<T> where T : IComponent
{
    [Dependency] protected readonly IAdminLogManager AdminLogManager = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly ChatSystem ChatSystem = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly StationSystem StationSystem = default!;

    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        Sawmill = Logger.GetSawmill("stationevents");
    }

    /// <inheritdoc/>
    protected override void Added(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        // Sunrise edit start - check for valid player station before announcing
        if (StationEventHelper.ShouldSkipEvent(uid, StationSystem, EntityManager, Sawmill))
        {
            return;
        }
        // Sunrise edit end

        AdminLogManager.Add(LogType.EventAnnounced, $"Event added / announced: {ToPrettyString(uid)}");

        // we don't want to send to players who aren't in game (i.e. in the lobby)
        Filter allPlayersInGame = Filter.Empty().AddWhere(GameTicker.UserHasJoinedGame);

        // Sunrise-Start
        if (stationEvent.StartAnnouncement != null)
            ChatSystem.DispatchFilteredAnnouncement(allPlayersInGame,
                Loc.GetString(stationEvent.StartAnnouncement),
                playDefault: false,
                announcementSound: stationEvent.StartAudio,
                colorOverride: stationEvent.StartAnnouncementColor);
        else
        {
            if (stationEvent.StartAudio != null)
            {
                Audio.PlayGlobal(stationEvent.StartAudio, allPlayersInGame, true);
            }
        }
        // Sunrise-End
    }

    /// <inheritdoc/>
    protected override void Started(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        AdminLogManager.Add(LogType.EventStarted, LogImpact.High, $"Event started: {ToPrettyString(uid)}");

        if (stationEvent.Duration != null)
        {
            var duration = stationEvent.MaxDuration == null
                ? stationEvent.Duration
                : TimeSpan.FromSeconds(RobustRandom.NextDouble(stationEvent.Duration.Value.TotalSeconds,
                    stationEvent.MaxDuration.Value.TotalSeconds));
            stationEvent.EndTime = Timing.CurTime + duration;
        }
    }

    /// <inheritdoc/>
    protected override void Ended(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        AdminLogManager.Add(LogType.EventStopped, $"Event ended: {ToPrettyString(uid)}");

        // we don't want to send to players who aren't in game (i.e. in the lobby)
        Filter allPlayersInGame = Filter.Empty().AddWhere(GameTicker.UserHasJoinedGame);

        // Sunrise-Start
        if (stationEvent.EndAnnouncement != null)
            ChatSystem.DispatchFilteredAnnouncement(allPlayersInGame,
                Loc.GetString(stationEvent.EndAnnouncement),
                playDefault: false,
                announcementSound: stationEvent.EndAudio,
                colorOverride: stationEvent.EndAnnouncementColor);
        else
        {
            if (stationEvent.StartAudio != null)
            {
                Audio.PlayGlobal(stationEvent.StartAudio, allPlayersInGame, true);
            }
        }
        // Sunrise-End
    }

    /// <summary>
    ///     Called every tick when this event is running.
    ///     Events are responsible for their own lifetime, so this handles starting and ending after time.
    /// </summary>
    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StationEventComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var stationEvent, out var ruleData))
        {
            if (!GameTicker.IsGameRuleAdded(uid, ruleData))
                continue;

            // Sunrise edit start - check for valid player station before starting
            if (!GameTicker.IsGameRuleActive(uid, ruleData) && !HasComp<DelayedStartRuleComponent>(uid))
            {
                if (StationEventHelper.ShouldSkipEvent(uid, StationSystem, EntityManager, Sawmill))
                {
                    GameTicker.EndGameRule(uid, ruleData);
                    continue;
                }

                GameTicker.StartGameRule(uid, ruleData);
            }
            else if (stationEvent.EndTime != null && Timing.CurTime >= stationEvent.EndTime && GameTicker.IsGameRuleActive(uid, ruleData))
            {
                GameTicker.EndGameRule(uid, ruleData);
            }
            // Sunrise edit end
        }
    }
}
