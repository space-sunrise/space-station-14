using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Shared.Atmos;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.FleshCult.FleshGrowth;

public sealed class SpreaderFleshSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    private const int GrowthsPerInterval = 5;
    private const float UpdateInterval = 1.0f;
    private const int DefaultDamageThreshold = 5;
    private const int MinMaxSpawnCount = 1;

    private float _accumulatedFrameTime;
    private readonly HashSet<EntityUid> _edgeGrowths = new();
    private EntityQuery<SpreaderFleshComponent> _spreaderQuery;
    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<MapGridComponent> _gridQuery;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpreaderFleshComponent, ComponentAdd>(SpreaderAddHandler);
        SubscribeLocalEvent<AirtightChanged>(OnAirtightChanged);

        _spreaderQuery = GetEntityQuery<SpreaderFleshComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
    }

    private void OnAirtightChanged(ref AirtightChanged ev)
    {
        UpdateNearbySpreaders(ev.Entity, ev.Airtight);
    }

    private void SpreaderAddHandler(EntityUid uid, SpreaderFleshComponent component, ComponentAdd args)
    {
        if (component.Enabled)
            _edgeGrowths.Add(uid);
    }

    public void UpdateNearbySpreaders(EntityUid blocker, AirtightComponent comp)
    {
        if (!_transformQuery.TryGetComponent(blocker, out var transform))
            return;

        if (!_gridQuery.TryGetComponent(transform.GridUid, out var grid))
            return;

        var tile = grid.TileIndicesFor(transform.Coordinates);

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection)(1 << i);
            if (!comp.AirBlockedDirection.IsFlagSet(direction))
                continue;

            var directionEnumerator = grid.GetAnchoredEntitiesEnumerator(
                SharedMapSystem.GetDirection(tile, direction.ToDirection()));

            while (directionEnumerator.MoveNext(out var ent))
            {
                if (_spreaderQuery.TryGetComponent(ent, out var s) && s.Enabled)
                    _edgeGrowths.Add(ent.Value);
            }
        }
    }

    public override void Update(float frameTime)
    {
        _accumulatedFrameTime += frameTime;

        if (_accumulatedFrameTime < UpdateInterval)
            return;

        _accumulatedFrameTime -= UpdateInterval;

        var growthList = _edgeGrowths.ToList();
        _robustRandom.Shuffle(growthList);

        var successes = 0;
        foreach (var entity in growthList)
        {
            if (!TryGrow(entity))
                continue;

            successes++;
            if (successes >= GrowthsPerInterval)
                break;
        }
    }

    private bool TryGrow(EntityUid ent, TransformComponent? transform = null, SpreaderFleshComponent? spreader = null)
    {
        if (!Resolve(ent, ref transform, ref spreader, false) || !spreader.Enabled)
            return false;

        if (!_gridQuery.TryGetComponent(transform.GridUid, out var grid))
            return false;

        var didGrow = false;

        for (var i = 0; i < 4; i++)
        {
            var direction = (DirectionFlag)(1 << i);
            var coords = transform.Coordinates.Offset(direction.AsDir().ToVec());

            if (grid.GetTileRef(coords).Tile.IsEmpty || _robustRandom.Prob(1 - spreader.Chance))
                continue;

            var ents = _mapSystem.GetLocal(transform.GridUid.Value, grid, coords);
            var entityUids = ents as EntityUid[] ?? ents.ToArray();

            if (entityUids.Any(x => IsTileBlockedFrom(x, direction)))
                continue;

            var (canSpawnWall, canSpawnFloor, entityStructureId) = AnalyzeTileEntities(entityUids);

            if (canSpawnFloor)
            {
                didGrow = SpawnFleshFloor(coords, spreader);
            }
            else if (canSpawnWall)
            {
                didGrow = SpawnFleshWall(coords, spreader, entityStructureId, entityUids);
            }
        }

        return didGrow;
    }

    private (bool canSpawnWall, bool canSpawnFloor, string entityStructureId) AnalyzeTileEntities(EntityUid[] entities)
    {
        var canSpawnWall = true;
        var canSpawnFloor = true;
        var entityStructureId = string.Empty;

        foreach (var entityUid in entities)
        {
            if (_tagSystem.HasAnyTag(entityUid, "Wall", "Window"))
            {
                if (!_tagSystem.HasAnyTag(entityUid, "Directional"))
                {
                    if (TryComp(entityUid, out MetaDataComponent? metaData) && metaData.EntityPrototype != null)
                    {
                        entityStructureId = metaData.EntityPrototype.ID;
                    }
                    canSpawnFloor = false;
                }
            }

            if (_tagSystem.HasAnyTag(entityUid, "Flesh", "Directional"))
            {
                canSpawnWall = false;
            }
        }

        return (canSpawnWall, canSpawnFloor, entityStructureId);
    }

    private bool SpawnFleshFloor(EntityCoordinates coords, SpreaderFleshComponent spreader)
    {
        var fleshFloor = EntityManager.SpawnEntity(spreader.GrowthResult, coords);
        var spreaderFleshComponent = EnsureComp<SpreaderFleshComponent>(fleshFloor);
        spreaderFleshComponent.Source = spreader.Source;
        return true;
    }

    private bool SpawnFleshWall(EntityCoordinates coords, SpreaderFleshComponent spreader, string entityStructureId, EntityUid[] existingEntities)
    {
        var fleshWall = EntityManager.SpawnEntity(spreader.WallResult, coords);
        var spreaderFleshComponent = EnsureComp<SpreaderFleshComponent>(fleshWall);
        spreaderFleshComponent.Source = spreader.Source;

        if (TryComp<DestructibleComponent>(fleshWall, out var destructible))
        {
            SetupDestructibleComponent(destructible, entityStructureId);
        }

        foreach (var entityUid in existingEntities)
        {
            if (_tagSystem.HasAnyTag(entityUid, "Wall", "Window"))
                EntityManager.DeleteEntity(entityUid);
        }

        return true;
    }

    private void SetupDestructibleComponent(DestructibleComponent destructible, string entityStructureId)
    {
        destructible.Thresholds.Clear();
        var damageThreshold = new DamageThreshold
        {
            Trigger = new DamageTrigger { Damage = DefaultDamageThreshold }
        };

        damageThreshold.AddBehavior(new SpawnEntitiesBehavior
        {
            Spawn = new Dictionary<EntProtoId, MinMax>
            {
                { entityStructureId, new MinMax { Min = MinMaxSpawnCount, Max = MinMaxSpawnCount } }
            },
            Offset = 0f
        });

        damageThreshold.AddBehavior(new DoActsBehavior
        {
            Acts = ThresholdActs.Destruction
        });

        destructible.Thresholds.Add(damageThreshold);
    }

    private bool IsTileBlockedFrom(EntityUid ent, DirectionFlag dir)
    {
        return _spreaderQuery.HasComponent(ent);
    }
}
