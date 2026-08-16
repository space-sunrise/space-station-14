using Content.Server._Sunrise.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Shuttles.Components;
using Content.Shared.Station.Components;

namespace Content.Server._Sunrise.GameTicking.Rules;

/// <summary>
/// Sends shuttle grids loaded by a game rule to a random station.
/// </summary>
public sealed partial class FTLToStationRuleSystem : GameRuleSystem<FTLToStationRuleComponent>
{
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FTLToStationRuleComponent, RuleLoadedGridsEvent>(OnRuleLoadedGrids);
    }

    private void OnRuleLoadedGrids(Entity<FTLToStationRuleComponent> ent, ref RuleLoadedGridsEvent args)
    {
        TrySendLoadedGrids(ent.AsNullable(), args.Grids);
    }

    /// <summary>
    /// Attempts to send the loaded grids to a random station.
    /// </summary>
    public bool TrySendLoadedGrids(Entity<FTLToStationRuleComponent?> ent, IReadOnlyList<EntityUid> grids)
    {
        if (!TryGetRandomStation(out var chosenStation))
            return false;

        if (!CanSendLoadedGrids(ent, chosenStation.Value, grids, out var targetGrid))
            return false;

        Resolve(ent, ref ent.Comp);
        DoSendLoadedGrids((ent.Owner, ent.Comp!), grids, targetGrid);
        return true;
    }

    /// <summary>
    /// Checks whether loaded grids can be sent to the selected station.
    /// </summary>
    public bool CanSendLoadedGrids(Entity<FTLToStationRuleComponent?> ent,
        EntityUid station,
        IReadOnlyList<EntityUid> grids,
        out EntityUid targetGrid)
    {
        targetGrid = default;
        if (!Resolve(ent, ref ent.Comp) || grids.Count == 0 || !TryComp<StationDataComponent>(station, out var stationData))
            return false;

        var largestGrid = _station.GetLargestGrid((station, stationData));
        if (largestGrid is null)
            return false;

        targetGrid = largestGrid.Value;
        return true;
    }

    private void DoSendLoadedGrids(Entity<FTLToStationRuleComponent> ent,
        IReadOnlyList<EntityUid> grids,
        EntityUid targetGrid)
    {
        foreach (var grid in grids)
            _shuttles.FTLToDock(
                grid,
                Comp<ShuttleComponent>(grid),
                targetGrid,
                0,
                ent.Comp.HyperspaceTime,
                ent.Comp.PriorityTag);
    }
}
