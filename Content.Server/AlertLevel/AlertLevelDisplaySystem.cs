using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Content.Shared.AlertLevel;
using Content.Shared.Power;

namespace Content.Server.AlertLevel;

public sealed class AlertLevelDisplaySystem : EntitySystem
{
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!; // Sunrise-Edit
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertChanged);
        SubscribeLocalEvent<AdditionalAlertLevelChangedEvent>(OnAdditionalAlertChanged); // Sunrise-Edit
        SubscribeLocalEvent<AlertLevelDisplayComponent, ComponentInit>(OnDisplayInit);
        SubscribeLocalEvent<AlertLevelDisplayComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnAlertChanged(AlertLevelChangedEvent args)
    {
        UpdateDisplays(args.Station); // Sunrise-Edit
    }

    // Sunrise added start - дисплеи показывают активный код с наивысшим визуальным приоритетом
    private void OnAdditionalAlertChanged(AdditionalAlertLevelChangedEvent args)
    {
        UpdateDisplays(args.Station);
    }

    private void UpdateDisplays(EntityUid station)
    {
        if (!_alertLevel.TryGetVisualAlertLevel((station, null), out var level, out _))
            return;

        var query = EntityQueryEnumerator<AlertLevelDisplayComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out _, out var appearance))
        {
            if (_stationSystem.GetOwningStation(uid) != station)
                continue;

            _appearance.SetData(uid, AlertLevelDisplay.CurrentLevel, level, appearance);
        }
    }
    // Sunrise added end

    private void OnDisplayInit(EntityUid uid, AlertLevelDisplayComponent alertLevelDisplay, ComponentInit args)
    {
        if (TryComp(uid, out AppearanceComponent? appearance))
        {
            var stationUid = _stationSystem.GetOwningStation(uid);
            if (stationUid != null
                && _alertLevel.TryGetVisualAlertLevel((stationUid.Value, null), out var level, out _))
            {
                _appearance.SetData(uid, AlertLevelDisplay.CurrentLevel, level, appearance);
            }
        }
    }
    private void OnPowerChanged(EntityUid uid, AlertLevelDisplayComponent alertLevelDisplay, ref PowerChangedEvent args)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        _appearance.SetData(uid, AlertLevelDisplay.Powered, args.Powered, appearance);
    }
}
