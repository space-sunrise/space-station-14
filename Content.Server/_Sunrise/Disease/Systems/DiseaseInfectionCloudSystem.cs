using Content.Server.Spreader;
using Content.Shared._Sunrise.Disease.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Disease.Systems;

public sealed class DiseaseInfectionCloudSystem : EntitySystem
{
    [Dependency] private readonly DiseaseSystem _disease = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    private static readonly EntProtoId CloudPrototype = "SunriseDiseaseInfectionCloud";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseInfectionCloudComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<DiseaseInfectionCloudComponent, SpreadNeighborsEvent>(OnSpreadNeighbors);
        SubscribeLocalEvent<DiseaseInfectionCloudComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(EntityUid uid, DiseaseInfectionCloudComponent component, ref ComponentGetState args)
    {
        if (component.Data == null)
            return;

        args.State = new DiseaseInfectionCloudComponentState(component.Data.Color);
    }

    private void OnStartCollide(Entity<DiseaseInfectionCloudComponent> ent, ref StartCollideEvent args)
    {
        TryInfectOnCollide((ent.Owner, ent.Comp), args.OtherEntity);
    }

    private void OnSpreadNeighbors(Entity<DiseaseInfectionCloudComponent> ent, ref SpreadNeighborsEvent args)
    {
        if (args.Updates <= 0)
            return;

        if (ent.Comp.Data == null || ent.Comp.SpreadAmount <= 0)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
            return;
        }

        if (args.NeighborFreeTiles.Count == 0)
            return;

        var randomTile = args.NeighborFreeTiles[_random.Next(args.NeighborFreeTiles.Count)];
        var coords = _map.GridTileToLocal(randomTile.Tile.GridUid, randomTile.Grid, randomTile.Tile.GridIndices);

        SpawnCloud(
            ent.Comp.Data!,
            coords,
            ent.Comp.CloudPrototype,
            ent.Comp.Source ?? ent.Owner,
            ent.Comp.SpreadAmount - 1);

        ent.Comp.SpreadAmount--;
        args.Updates--;

        if (ent.Comp.SpreadAmount <= 0)
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
    }

    public bool TryInfectOnCollide(Entity<DiseaseInfectionCloudComponent?> cloud, EntityUid target)
    {
        if (!Resolve(cloud, ref cloud.Comp, false))
            return false;

        if (!CanInfectOnCollide(cloud, target))
            return false;

        _disease.ProbInfect(cloud.Comp.Data!, target, cloud.Comp.Source ?? cloud.Owner, cloud.Comp.InfectionChance);
        return true;
    }

    public bool CanInfectOnCollide(Entity<DiseaseInfectionCloudComponent?> cloud, EntityUid target)
    {
        if (!Resolve(cloud, ref cloud.Comp, false))
            return false;

        if (target == cloud.Owner)
            return false;

        return cloud.Comp.Data != null;
    }

    public bool TrySpawnCloud(
        DiseaseData disease,
        EntityCoordinates coordinates,
        out EntityUid cloud,
        EntityUid? source = null,
        int spreadAmount = 4,
        float checkRange = 0.01f)
    {
        return TrySpawnCloud(disease, coordinates, CloudPrototype, out cloud, source, spreadAmount, checkRange);
    }

    public bool TrySpawnCloud(
        DiseaseData disease,
        EntityCoordinates coordinates,
        EntProtoId cloudPrototype,
        out EntityUid cloud,
        EntityUid? source = null,
        int spreadAmount = 4,
        float checkRange = 0.01f)
    {
        cloud = EntityUid.Invalid;

        if (!CanSpawnCloud(coordinates, checkRange))
            return false;

        cloud = SpawnCloud(disease, coordinates, cloudPrototype, source, spreadAmount);
        return true;
    }

    public bool CanSpawnCloud(EntityCoordinates coordinates, float checkRange = 0.01f)
    {
        foreach (var _ in _entityLookup.GetEntitiesInRange<DiseaseInfectionCloudComponent>(coordinates, checkRange))
        {
            return false;
        }

        return true;
    }

    private EntityUid SpawnCloud(
        DiseaseData disease,
        EntityCoordinates coordinates,
        EntProtoId cloudPrototype,
        EntityUid? source = null,
        int spreadAmount = 4)
    {
        var uid = Spawn(cloudPrototype, coordinates);

        if (!TryComp(uid, out DiseaseInfectionCloudComponent? cloud))
            return uid;

        cloud.Data = (DiseaseData)disease.CloneForInfection();
        cloud.Source = source;
        cloud.InfectionChance = _disease.GetInfectionInfectivity(source ?? uid, cloud.Data);
        cloud.SpreadAmount = spreadAmount;
        cloud.CloudPrototype = cloudPrototype;

        if (cloud.SpreadAmount > 0)
        {
            var component = EnsureComp<ActiveEdgeSpreaderComponent>(uid);
            Dirty(uid, component);
        }
        else
            RemCompDeferred<ActiveEdgeSpreaderComponent>(uid);

        return uid;
    }

    public void UpdateInfectionForStrain(DiseaseData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.StrainId))
            return;

        var query = EntityQueryEnumerator<DiseaseInfectionCloudComponent>();
        while (query.MoveNext(out var uid, out var cloud))
        {
            if (cloud.Data == null || cloud.Data.StrainId != data.StrainId)
                continue;

            cloud.InfectionChance = _disease.GetInfectionInfectivity(cloud.Source ?? uid, cloud.Data);
            cloud.Data = (DiseaseData)data.CloneForInfection();
            Dirty(uid, cloud);
        }
    }
}