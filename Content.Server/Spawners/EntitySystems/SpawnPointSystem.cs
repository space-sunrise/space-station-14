using Content.Server.GameTicking;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared._Sunrise.Misc; // Sunrise-Edit

namespace Content.Server.Spawners.EntitySystems;

public sealed class SpawnPointSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null)
            return;

        // TODO: Cache all this if it ends up important.
        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();

        while (points.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            // Sunrise-Start
            var hasRoundStart = EntityManager.HasComponent<RoundStartSpawnPointComponent>(uid);
            var hasLateJoin = EntityManager.HasComponent<LateJoinSpawnPointComponent>(uid);
            if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
            {
                if (hasLateJoin)
                    continue;
            }
            else if (_gameTicker.RunLevel == GameRunLevel.InRound)
            {
                if (hasRoundStart)
                    continue;
            }
            // Sunrise-End

            if (args.Station != null && _stationSystem.GetOwningStation(uid, xform) != args.Station)
                continue;

            // Delta-V: Allow setting a desired SpawnPointType
            if (args.DesiredSpawnPointType != SpawnPointType.Unset)
            {
                var isMatchingJob = spawnPoint.SpawnType == SpawnPointType.Job &&
                                    (args.Job == null || spawnPoint.Job == args.Job);

                switch (args.DesiredSpawnPointType)
                {
                    case SpawnPointType.Job when isMatchingJob:
                    case SpawnPointType.LateJoin when spawnPoint.SpawnType == SpawnPointType.LateJoin:
                    case SpawnPointType.Observer when spawnPoint.SpawnType == SpawnPointType.Observer:
                        possiblePositions.Add(xform.Coordinates);
                        break;
                    default:
                        continue;
                }
            }

            if (_gameTicker.RunLevel == GameRunLevel.InRound &&
                spawnPoint.SpawnType == SpawnPointType.LateJoin &&
                args.DesiredSpawnPointType != SpawnPointType.Job)
            {
                possiblePositions.Add(xform.Coordinates);
            }

            if ((_gameTicker.RunLevel != GameRunLevel.InRound || args.DesiredSpawnPointType == SpawnPointType.Job) &&
                spawnPoint.SpawnType == SpawnPointType.Job &&
                (args.Job == null || spawnPoint.Job == args.Job))
            {
                possiblePositions.Add(xform.Coordinates);
            }
        }

        // Sunrise-Start
        if (possiblePositions.Count == 0)
        {
            var points3 = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
            while (points3.MoveNext(out var uid, out var spawnPoint, out var xform))
            {
                if (spawnPoint.SpawnType != SpawnPointType.LateJoin)
                    continue;
                if (_stationSystem.GetOwningStation(uid, xform) == args.Station)
                    possiblePositions.Add(xform.Coordinates);
            }
        }
        // Sunrise-End

        if (possiblePositions.Count == 0)
        {
            // Ok we've still not returned, but we need to put them /somewhere/.
            // TODO: Refactor gameticker spawning code so we don't have to do this!
            var points2 = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();

            if (points2.MoveNext(out var spawnPoint, out var xform))
            {
                possiblePositions.Add(xform.Coordinates);
            }
            else
            {
                Log.Error("No spawn points were available!");
                return;
            }
        }

        var spawnLoc = _random.Pick(possiblePositions);

        args.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);
    }
}
