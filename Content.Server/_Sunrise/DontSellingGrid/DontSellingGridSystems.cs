using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Events;
using Content.Shared._Sunrise.Economy;
using Content.Shared.Cargo;
using Content.Shared.GameTicking;
using Robust.Shared.Containers;

namespace Content.Server._Sunrise.DontSellingGrid;

public sealed class StationDontSellingSystems : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DontSellingGridComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StationDontSellingGridComponent, StationPostInitEvent>(OnPostInit);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawning);
        SubscribeLocalEvent<DontSellComponent, PriceCalculationEvent>(OnCalculatePrice);
    }

    private void OnCalculatePrice(EntityUid uid, DontSellComponent component, ref PriceCalculationEvent args)
    {
        args.Price = 0;
        args.Handled = true;
    }

    private void OnStartup(EntityUid uid, DontSellingGridComponent component, ref ComponentStartup args)
    {
        var entities = new HashSet<Entity<StaticPriceComponent>>();
        _lookup.GetGridEntities(uid, entities);
        foreach (var entityUid in entities)
        {
            DepreciatePrice(entityUid);
        }
    }

    private void DepreciatePrice(EntityUid uid)
    {
        EnsureComp<DontSellComponent>(uid);

        if (!TryComp<ContainerManagerComponent>(uid, out var containers))
            return;

        foreach (var container in containers.Containers.Values)
        {
            foreach (var ent in container.ContainedEntities)
            {
                DepreciatePrice(ent);
            }
        }
    }

    private void OnPlayerSpawning(PlayerSpawnCompleteEvent ev)
    {
        if (!HasComp<StationDontSellingGridComponent>(ev.Station))
            return;

        DepreciatePrice(ev.Mob);
    }

    private void OnPostInit(EntityUid uid, StationDontSellingGridComponent component, ref StationPostInitEvent args)
    {
        foreach (var gridUid in args.Station.Comp.Grids)
        {
            AddComp<DontSellingGridComponent>(gridUid);
        }
    }
}
