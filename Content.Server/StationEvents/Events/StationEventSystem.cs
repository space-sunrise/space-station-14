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

            // Sunrise-Start: Исключаем ЦентКом из станционных событий
            if (!GameTicker.IsGameRuleActive(uid, ruleData) && !HasComp<DelayedStartRuleComponent>(uid))
            {
                // Проверяем, не является ли целевая станция ЦентКомом
                var targetStation = StationSystem.GetStations()
                    .FirstOrDefault(station =>
                    {
                        if (!TryComp<StationDataComponent>(station, out var data))
                            return false;

                        if (data.StationConfig != null && data.StationConfig.StationPrototype == "NanotrasenCentralCommand")
                            return false;

                        return true;
                    });

                if (!targetStation.IsValid())
                {
                    Sawmill.Info($"Skipping event {ToPrettyString(uid)}: no valid target station");
                    GameTicker.EndGameRule(uid, ruleData);
                    continue;
                }

                GameTicker.StartGameRule(uid, ruleData);
            }
            else if (stationEvent.EndTime != null && Timing.CurTime >= stationEvent.EndTime && GameTicker.IsGameRuleActive(uid, ruleData))
            {
                GameTicker.EndGameRule(uid, ruleData);
            }
            // Sunrise-End
        }
    }
}
