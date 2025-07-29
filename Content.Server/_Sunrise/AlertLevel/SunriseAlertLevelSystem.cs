using Content.Server.GameTicking;
using Robust.Shared.Log;

namespace Content.Server.AlertLevel;

/// <summary>
/// Sunrise-specific alert level system that handles Epsilon alert level events.
/// </summary>
public sealed class SunriseAlertLevelSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private const string EpsilonAlertLevel = "epsilon";
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("sunrise-alert-level");
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
        _sawmill.Info("SunriseAlertLevelSystem initialized");
    }

    private void OnAlertLevelChanged(AlertLevelChangedEvent ev)
    {
        _sawmill.Info($"Alert level changed to {ev.AlertLevel} on station {ev.Station}");

        if (ev.AlertLevel == EpsilonAlertLevel)
        {
            _sawmill.Info($"Epsilon alert level triggered on station {ev.Station}, adding Death Squad Lawset event");
            _gameTicker.AddGameRule("EpsilonDeathSquadLawset");
        }
    }
}
