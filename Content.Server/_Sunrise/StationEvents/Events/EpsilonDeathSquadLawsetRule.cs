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
    [Dependency] private readonly StationSystem _stationSystem = default!;

    private const string DeathSquadLawsetId = "DeathSquadLawset";

    private EntityUid? _targetStation;

    protected override void Started(EntityUid uid,
        EpsilonDeathSquadLawsetComponent comp,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);
        if (_targetStation == null)
        {
            Sawmill.Error($"Target station not set for EpsilonDeathSquadLawsetRule");
            return;
        }

        var lawsetId = DeathSquadLawsetId;
        if (!_prototypeManager.TryIndex<SiliconLawsetPrototype>(lawsetId, out var lawsetProto))
        {
            Sawmill.Error($"Could not find lawset prototype: {lawsetId}");
            return;
        }

        // Convert the prototype's law IDs to actual law objects using LINQ
        var laws = lawsetProto.Laws
            .Select(lawId => _prototypeManager.Index<SiliconLawPrototype>(lawId))
            .Select(lawProto => new SiliconLaw
            {
                LawString = Loc.GetString(lawProto.LawString),
                Order = lawProto.Order
            })
            .ToList();

        Sawmill.Debug($"Converted {laws.Count} laws");

        var borgCount = 0;
        var changedCount = 0;
        var query = EntityQueryEnumerator<SiliconLawProviderComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var provider, out var xform))
        {
            borgCount++;
            var borgGrid = xform.GridUid;
            Sawmill.Debug($"Found borg {ent} on grid {borgGrid}");

            // Skip borgs with blocked law changes
            if (HasComp<BlockLawChangeComponent>(ent))
            {
                Sawmill.Info($"Skipping borg {ent} - has BlockLawChangeComponent");
                continue;
            }

            // Only change laws for borgs on grids that belong to the chosen station
            if (borgGrid == null || !_stationSystem.TryGetOwningStation(borgGrid, out var owningStation) || owningStation != _targetStation)
            {
                Sawmill.Debug(
                    $"Skipping borg {ent} - not on target station (on grid {borgGrid}, owning station: {owningStation})");
                continue;
            }

            Sawmill.Debug($"Changing laws for borg {ent}");
            _siliconLaw.SetLaws(laws, ent, provider.LawUploadSound);
            changedCount++;
        }

        Sawmill.Debug($"EpsilonDeathSquadLawsetRule completed: found {borgCount} borgs, changed laws for {changedCount} borgs");
    }

    public void SetTargetStation(EntityUid ruleEntity, EntityUid station)
    {
        _targetStation = station;
    }
}
