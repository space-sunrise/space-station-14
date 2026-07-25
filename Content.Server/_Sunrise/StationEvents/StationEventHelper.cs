using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Station.Components;
using Robust.Shared.GameObjects;

namespace Content.Server._Sunrise.StationEvents;

/// <summary>
/// Helper class for checking player station availability (excluding CentComm)
/// </summary>
public static class StationEventHelper
{
    /// <summary>
    /// Determines whether the event should be skipped due to no valid player station being available.
    /// </summary>
    public static bool ShouldSkipEvent(
        EntityUid uid,
        StationSystem stationSystem,
        IEntityManager entityManager,
        ISawmill sawmill)
    {
        if (!HasValidTargetStation(stationSystem, entityManager))
        {
            sawmill.Info($"Skipping event {entityManager.ToPrettyString(uid)}: no valid target station (CentComm excluded)");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks whether a valid player station (excluding CentComm) exists.
    /// </summary>
    public static bool HasValidTargetStation(StationSystem stationSystem, IEntityManager entityManager)
    {
        var stations = stationSystem.GetStations();

        foreach (var station in stations)
        {
            if (!entityManager.TryGetComponent<StationDataComponent>(station, out var data))
                continue;

            // Skip stations without config
            if (data.StationConfig is null)
                continue;

            // Exclude CentComm by StationPrototype
            if (data.StationConfig.StationPrototype == "NanotrasenCentralCommand")
                continue;

            return true;
        }

        return false;
    }
}
