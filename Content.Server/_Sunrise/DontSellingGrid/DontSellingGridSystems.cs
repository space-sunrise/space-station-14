using Content.Server.Cargo.Components;
using Content.Server.Station.Events;
using Content.Shared._Sunrise.Economy;
using Content.Shared.Cargo;
using Content.Shared.GameTicking;
using Robust.Shared.Containers;

namespace Content.Server._Sunrise.DontSellingGrid;

public sealed class StationDontSellingSystems : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private readonly HashSet<Entity<StaticPriceComponent>> _entities = [];

    private EntityQuery<ContainerManagerComponent> _containerQuery;
    private EntityQuery<StationDontSellingGridComponent> _stationQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DontSellingGridComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StationDontSellingGridComponent, StationPostInitEvent>(OnPostInit);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawning);
        SubscribeLocalEvent<DontSellComponent, PriceCalculationEvent>(OnCalculatePrice);

        _containerQuery = GetEntityQuery<ContainerManagerComponent>();
        _stationQuery = GetEntityQuery<StationDontSellingGridComponent>();
    }

    #region Event handlers

    private void OnStartup(Entity<DontSellingGridComponent> ent, ref ComponentStartup args)
    {
        _entities.Clear();
        _lookup.GetGridEntities(ent, _entities);
        foreach (var entityUid in _entities)
        {
            if (!entityUid.Owner.IsValid())
                continue;

            DepreciatePrice(entityUid);
        }
    }

    private void OnPostInit(Entity<StationDontSellingGridComponent> ent, ref StationPostInitEvent args)
    {
        foreach (var gridUid in args.Station.Comp.Grids)
        {
            if (!gridUid.IsValid())
                continue;

            EnsureComp<DontSellingGridComponent>(gridUid);
        }
    }

    private void OnPlayerSpawning(PlayerSpawnCompleteEvent ev)
    {
        if (!_stationQuery.HasComp(ev.Station))
            return;

        DepreciatePrice(ev.Mob);
    }

    private void OnCalculatePrice(Entity<DontSellComponent> ent, ref PriceCalculationEvent args)
    {
        args.Price = 0;
        args.Handled = true;
    }

    #endregion

    #region Decrease price logic

    private void DepreciatePrice(EntityUid uid)
    {
        if (!uid.IsValid())
            return;

        EnsureComp<DontSellComponent>(uid);

        if (!_containerQuery.TryComp(uid, out var containers))
            return;

        foreach (var container in containers.Containers.Values)
        {
            foreach (var ent in container.ContainedEntities)
            {
                DepreciatePrice(ent);
            }
        }
    }

    #endregion
}
