using System.Linq;
using Content.Server.Silicons.Laws;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Prototypes;
using EpsilonDeathSquadLawsetComponent = Content.Server._Sunrise.StationEvents.Components.EpsilonDeathSquadLawsetComponent;

namespace Content.Server._Sunrise.StationEvents.Events;

public sealed class EpsilonDeathSquadLawsetRule : StationEventSystem<EpsilonDeathSquadLawsetComponent>
{
    [Dependency] private readonly SiliconLawSystem _siliconLaw = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private const string DeathSquadLawsetId = "DeathSquadLawset";


    protected override void Started(EntityUid uid,
        EpsilonDeathSquadLawsetComponent comp,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);
       var targetStation = StationSystem.GetOwningStation(uid);
       var nukeXform = Transform(uid);
       var stationUid = StationSystem.GetStationInMap(nukeXform.MapID);
        Sawmill.Info("EpsilonDeathSquadLawsetRule started" + targetStation);
        Sawmill.Info("A+" +stationUid);
        // Get the target station from the component
        if (targetStation == EntityUid.Invalid)
        {
            Sawmill.Warning("No target station specified for Epsilon Death Squad Lawset");
            return;
        }

        // Get the station data to access its grids
        if (!TryComp<StationDataComponent>(targetStation, out var stationData))
        {
            Sawmill.Error($"Could not get station data for station {targetStation}");
            return;
        }

        Sawmill.Info($"Target station for law changes: {targetStation} with grids: {string.Join(", ", stationData.Grids)}");

        var lawsetId = DeathSquadLawsetId;
        if (!_prototypeManager.TryIndex<SiliconLawsetPrototype>(lawsetId, out var lawsetProto))
        {
            Sawmill.Error($"Could not find lawset prototype: {lawsetId}");
            return;
        }

        Sawmill.Info($"Found lawset prototype: {lawsetId} with {lawsetProto.Laws.Count} laws");

        // Convert the prototype's law IDs to actual law objects using LINQ
        var laws = lawsetProto.Laws
            .Select(lawId => _prototypeManager.Index<SiliconLawPrototype>(lawId))
            .Select(lawProto => new SiliconLaw
            {
                LawString = Loc.GetString(lawProto.LawString),
                Order = lawProto.Order
            })
            .ToList();

        Sawmill.Info($"Converted {laws.Count} laws");

        var borgCount = 0;
        var changedCount = 0;
        var query = EntityQueryEnumerator<SiliconLawProviderComponent>();
        var stationGrids = stationData.Grids;
        while (query.MoveNext(out var ent, out var provider))
        {
            borgCount++;
            var borgGrid = Transform(ent).GridUid;
            Sawmill.Info($"Found borg {ent} on grid {borgGrid}");

            // Skip borgs with blocked law changes
            if (HasComp<BlockLawChangeComponent>(ent))
            {
                Sawmill.Info($"Skipping borg {ent} - has BlockLawChangeComponent");
                continue;
            }

            // Only change laws for borgs on grids that belong to the chosen station
            if (borgGrid == null || !stationGrids.Contains(borgGrid.Value))
            {
                Sawmill.Info(
                    $"Skipping borg {ent} - not on station grids (on {borgGrid}, station grids: {string.Join(", ", stationData.Grids)})");
                continue;
            }

            Sawmill.Info($"Changing laws for borg {ent}");
            _siliconLaw.SetLaws(laws, ent, provider.LawUploadSound);
            changedCount++;
        }

        Sawmill.Info($"EpsilonDeathSquadLawsetRule completed: found {borgCount} borgs, changed laws for {changedCount} borgs");
    }
}
