using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Spawners.EntitySystems;

public sealed class ContainerSpawnPointSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawningEvent>(HandlePlayerSpawning, before: new []{ typeof(SpawnPointSystem) });
    }

    public void HandlePlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null)
            return;

        if (args.DesiredSpawnPointType == SpawnPointType.Observer)
            return;

        var query = EntityQueryEnumerator<ContainerSpawnPointComponent, ContainerManagerComponent, TransformComponent>();
        var cryoContainers = new List<Entity<ContainerSpawnPointComponent, ContainerManagerComponent, TransformComponent>>();

        while (query.MoveNext(out var uid, out var spawnPoint, out var container, out var xform))
        {
            if (args.Station != null && _station.GetOwningStation(uid, xform) != args.Station)
                continue;

            // If it's unset, then we allow it to be used for both roundstart and midround joins
            if (spawnPoint.SpawnType == SpawnPointType.Unset)
            {
                // make sure we also check the job here for various reasons.
                if (spawnPoint.Job == null || spawnPoint.Job == args.Job)
                    cryoContainers.Add((uid, spawnPoint, container, xform));
                continue;
            }

            if (_gameTicker.RunLevel == GameRunLevel.InRound &&
                spawnPoint.SpawnType == SpawnPointType.LateJoin &&
                args.DesiredSpawnPointType != SpawnPointType.Job)
            {
                cryoContainers.Add((uid, spawnPoint, container, xform));
            }

            if ((_gameTicker.RunLevel != GameRunLevel.InRound || args.DesiredSpawnPointType == SpawnPointType.Job) &&
                spawnPoint.SpawnType == SpawnPointType.Job &&
                (args.Job == null || spawnPoint.Job == args.Job))
            {
                cryoContainers.Add((uid, spawnPoint, container, xform));
            }
        }

        if (cryoContainers.Count == 0)
            return;

        _random.Shuffle(cryoContainers);
        var spawnCoords = cryoContainers[0].Comp3.Coordinates;

        args.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnCoords,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);

        foreach (var (uid, spawnPoint, manager, xform) in cryoContainers)
        {
            if (!_container.TryGetContainer(uid, spawnPoint.ContainerId, out var container, manager))
                continue;

            if (_container.Insert(args.SpawnResult.Value, container, containerXform: xform))
                break;
        }

        // Даже если не удалось поместить в контейнер - моб уже заспавнен на координатах криокапсулы
    }
}
