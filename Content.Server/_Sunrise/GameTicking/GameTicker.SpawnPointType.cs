using Content.Server.Spawners.Components;
using Content.Server._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared.Roles;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала
namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    partial void SelectSpawnPointTypePortal(
        EntityUid station,
        JobPrototype job,
        bool lateJoin,
        ref SpawnPointType spawnPointType);

    partial void SelectSpawnPointTypePortal(
        EntityUid station,
        JobPrototype job,
        bool lateJoin,
        ref SpawnPointType spawnPointType)
    {
        if (lateJoin && job.AlwaysUseSpawner)
            spawnPointType = SpawnPointType.Job;

        var joinKind = lateJoin ? PlayerJoinKind.LateJoin : PlayerJoinKind.RoundStart;
        spawnPointType = _playerJoinableMap.GetSpawnPointType((station, null), joinKind, spawnPointType);
    }
}
