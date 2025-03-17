using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Content.Server.Station.Systems;
using Content.Server.AlertLevel;
using Content.Shared.Station.Components;
using Timer = Robust.Shared.Timing.Timer;
using Content.Server.Chat.Systems;
using System.Threading;

namespace Content.Server.Access.Systems; //Sunrise-edited

public sealed class AccessSystem : SharedAccessSystem
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertAccessesEvent>(OnAlertLevelChanged);
    }
    private readonly CancellationTokenSource? _timerCancel;

    /// <summary>
    /// Запускает таймер и выводит объявление о смене доступов через 1 минуту
    /// </summary>

    private void OnAlertLevelChanged(AlertAccessesEvent ev)
    {
        if (!TryComp<AlertLevelComponent>(ev.Station, out var alert))
            return;

        if (alert.AlertLevels == null)
            return;

        var levels = new Dictionary<string, string>
        {
            { "green", "access-system-accesses-delay-green" },
            { "blue", "access-system-accesses-delay-blue" },
            { "red", "access-system-accesses-delay-red" },
            { "yellow", "access-system-accesses-delay-yellow" },
            { "gamma", "access-system-accesses-delay-gamma" },
        };

        foreach (var announce in levels)
        {
            if (alert.CurrentLevel.Contains(announce.Key))
            {
                Timer.Spawn(TimeSpan.FromMinutes(1), () => AlertAccessesDelay(ev), _timerCancel?.Token ?? default);
                _chatSystem.DispatchStationAnnouncement(ev.Station,
                    Loc.GetString(announce.Value),
                    playDefault: true,
                    colorOverride: Color.Yellow,
                    sender: Loc.GetString("access-system-sender"));
            }
        }
        if (alert.CurrentLevel == "delta")
            AlertAccessesDelay(ev);

    }

    /// <summary>
    /// Устанавливает доступы спустя 1 минуту после начала таймера
    /// </summary>
    private void AlertAccessesDelay(AlertAccessesEvent ev)
    {
        _chatSystem.DispatchStationAnnouncement(ev.Station,
            Loc.GetString("access-system-accesses-established"),
            playDefault: true,
            colorOverride: Color.Yellow,
            sender: Loc.GetString("access-system-sender"));

        var query = EntityQueryEnumerator<AccessReaderComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var reader, out var xform))
        {
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != ev.Station)
                continue;

            if (!TryComp<AccessReaderComponent>(uid, out var comp))
                return;

            if (comp.AlertAccesses.Count == 0)
                continue;

            Update((uid, reader));
            Dirty(uid, reader);
            _timerCancel?.Cancel();
        }
    }

    /// <summary>
    /// Устанавливает значение из прототипа в зависимости от кода
    /// </summary>
    public void Update(Entity<AccessReaderComponent> entity)
    {

        if (!TryComp<AlertLevelComponent>(_station.GetOwningStation(entity.Owner), out var alerts))
            return;

        if (alerts.AlertLevels == null)
            return;

        var alertLevels = new Dictionary<string, AccessReaderComponent.CurrentAlertLevel>
        {
            { "blue", AccessReaderComponent.CurrentAlertLevel.blue },
            { "red", AccessReaderComponent.CurrentAlertLevel.red },
            { "yellow", AccessReaderComponent.CurrentAlertLevel.yellow },
            { "gamma", AccessReaderComponent.CurrentAlertLevel.gamma },
            { "delta", AccessReaderComponent.CurrentAlertLevel.delta }
        };

        entity.Comp.Group = string.Empty; // Значение по умолчанию
        foreach (var level in alertLevels)
        {
            if (alerts.CurrentLevel.Contains(level.Key))
            {
                if (entity.Comp.AlertAccesses.TryGetValue(level.Value, out var value))
                {
                    entity.Comp.Group = value;
                    break;
                }
            }
        }
    }
}
