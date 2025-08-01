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
        // Теперь можете использовать comp.TargetStation
        var targetStation = comp.TargetStation;

        // Если TargetStation не установлена, можете установить случайную
        if (targetStation == EntityUid.Invalid)
        {
            if (!TryGetRandomStation(out var chosenStation))
                return;
            targetStation = chosenStation.Value;
            comp.TargetStation = targetStation; // Сохраняем для последующего использования
        }

        // Проверяем, что у нас есть валидная станция
        if (!TryComp<StationDataComponent>(targetStation, out var stationData))
        {
            Logger.GetSawmill("station-event").Error($"Target station {targetStation} does not have StationDataComponent");
            return;
        }
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

    public void SetTargetStation(EntityUid ruleEntity, EntityUid station)
    {
        if (TryComp<EpsilonDeathSquadLawsetComponent>(ruleEntity, out var comp))
        {
            comp.TargetStation = station;
        }
    }
}
