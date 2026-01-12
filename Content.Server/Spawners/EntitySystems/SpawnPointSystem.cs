using Content.Server.GameTicking;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Prototypes; // Sunrise-Edit
using Content.Shared.Roles; // Sunrise-Edit

namespace Content.Server.Spawners.EntitySystems;

public sealed class SpawnPointSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!; // Sunrise-Edit

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null)
            return;

        // TODO: Cache all this if it ends up important.

        // Sunrise-Start
        // Check job prototype flags (AlwaysUseSpawner) before scanning spawn points.
        var jobAlwaysUse = false;
        if (args.Job != null)
        {
            if (_prototypeManager.TryIndex(args.Job.Value, out JobPrototype? jobProto) && jobProto.AlwaysUseSpawner)
            {
                jobAlwaysUse = true;
            }
        }
        // Sunrise-End

        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();

        // Sunrise-Start
        // Cache collections for fallback optimization - collect all spawner positions in one pass
        var jobPositionsForRole = new List<EntityCoordinates>();
        var allLateJoinPositions = new List<EntityCoordinates>();
        // Sunrise-End

        while (points.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            if (args.Station != null && _stationSystem.GetOwningStation(uid, xform) != args.Station)
                continue;

            // Sunrise-Start
            // Collect positions for fallback use (optimization: single pass instead of multiple queries)
            if (spawnPoint.SpawnType == SpawnPointType.Job && spawnPoint.Job == args.Job)
                jobPositionsForRole.Add(xform.Coordinates);
            if (spawnPoint.SpawnType == SpawnPointType.LateJoin)
                allLateJoinPositions.Add(xform.Coordinates);
            // Sunrise-End

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
                        continue; // Sunrise added edit: Skip remaining checks to avoid duplicates
                    default:
                        break; // Sunrise added edit: Continue to check other conditions
                }
            }

            if (_gameTicker.RunLevel == GameRunLevel.InRound &&
                spawnPoint.SpawnType == SpawnPointType.LateJoin &&
                // Sunrise added start
                args.DesiredSpawnPointType != SpawnPointType.Job &&
                !jobAlwaysUse)
                // Sunrise added end
            {
                possiblePositions.Add(xform.Coordinates);
            }

            if ((_gameTicker.RunLevel != GameRunLevel.InRound || args.DesiredSpawnPointType == SpawnPointType.Job || jobAlwaysUse) && // Sunrise-Edit
                spawnPoint.SpawnType == SpawnPointType.Job &&
                (args.Job == null || spawnPoint.Job == args.Job))
            {
                possiblePositions.Add(xform.Coordinates);
            }
        }

        // Sunrise-Start
        // Fallback 1: Use pre-collected job spawners for this specific role
        if (possiblePositions.Count == 0 && args.Job != null)
        {
            possiblePositions.AddRange(jobPositionsForRole);
        }

        // Fallback 2: Use pre-collected late join spawners as last resort
        if (possiblePositions.Count == 0)
        {
            possiblePositions.AddRange(allLateJoinPositions);
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
