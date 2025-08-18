using Content.Server.Station.Systems;
using Content.Server._Sunrise.StationEvents.Components;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.GameObjects;

namespace Content.Server._Sunrise.StationEvents.Events;

/// <summary>
/// Game rule for changing borg laws to Epsilon during Epsilon alert level.
/// </summary>
public sealed class EpsilonDeathSquadLawsetRule : EntitySystem
{
    private EntityUid? _targetStation;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public void SetTargetStation(EntityUid ruleEntity, EntityUid station)
    {
        _targetStation = station;
    }

    public override void Update(float frameTime)
    {
        if (_targetStation == null)
            return;
        // Example: apply law changes to all borgs on the station
        // ...
    }
}
