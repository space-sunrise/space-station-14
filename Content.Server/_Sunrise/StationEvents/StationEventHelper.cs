using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.GameObjects;

namespace Content.Server._Sunrise.StationEvents;

/// <summary>
/// Вспомогательный класс для проверки доступности игровой станции (исключая ЦентКом)
/// </summary>
public static class StationEventHelper
{
    private static EntityUid _cachedStation = EntityUid.Invalid;
    private static bool _cacheValid;

    /// <summary>
    /// Проверяет, нужно ли пропустить событие из-за отсутствия доступной игровой станции
    /// </summary>
    /// <param name="uid">Идентификатор события</param>
    /// <param name="stationSystem">Система станций</param>
    /// <param name="entityManager">Менеджер сущностей</param>
    /// <param name="sawmill">Логгер</param>
    /// <returns>true, если событие нужно пропустить</returns>
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
    /// Проверяет наличие доступной игровой станции (не ЦентКом)
    /// </summary>
    /// <param name="stationSystem">Система станций</param>
    /// <param name="entityManager">Менеджер сущностей</param>
    /// <returns>true, если есть доступная станция</returns>
    public static bool HasValidTargetStation(StationSystem stationSystem, IEntityManager entityManager)
    {
        if (_cacheValid)
            return _cachedStation.IsValid();

        var stations = stationSystem.GetStations();

        foreach (var station in stations)
        {
            if (!entityManager.TryGetComponent<StationDataComponent>(station, out var data))
                continue;

            // Исключаем ЦентКом по StationPrototype
            if (data.StationConfig != null && data.StationConfig.StationPrototype == "NanotrasenCentralCommand")
                continue;

            _cachedStation = station;
            _cacheValid = true;
            return true;
        }

        _cachedStation = EntityUid.Invalid;
        _cacheValid = true;
        return false;
    }

    /// <summary>
    /// Сбрасывает кеш (вызывается при изменении списка станций)
    /// </summary>
    public static void InvalidateCache()
    {
        _cacheValid = false;
        _cachedStation = EntityUid.Invalid;
    }
}
