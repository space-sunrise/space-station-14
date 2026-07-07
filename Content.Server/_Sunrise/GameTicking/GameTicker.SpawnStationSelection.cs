using Content.Server._Sunrise.Other.StationOnlyDirectSpawn;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала
namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    partial void FilterFallbackSpawnableStationsPortal(List<EntityUid> stations)
    {
        stations.RemoveAll(station => HasComp<StationOnlyDirectSpawnComponent>(station));
    }
}
