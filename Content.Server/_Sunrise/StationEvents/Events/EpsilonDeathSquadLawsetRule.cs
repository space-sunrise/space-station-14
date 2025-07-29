using System.Linq;
using Content.Server.Silicons.Laws;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Emag.Systems;
using Content.Shared.Tag;

namespace Content.Server.StationEvents.Events;

public sealed class EpsilonDeathSquadLawsetRule : StationEventSystem<StationEventComponent>
{
    [Dependency] private readonly SiliconLawSystem _siliconLaw = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private const string DeathSquadLawsetId = "DeathSquadLawset";

    protected override void Started(EntityUid uid,
        StationEventComponent comp,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

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

        var query = EntityQueryEnumerator<SiliconLawProviderComponent>();
        while (query.MoveNext(out var ent, out var provider))
        {
            // Skip borgs with blocked law changes
            if (HasComp<BlockLawChangeComponent>(ent))
                continue;

            // Only change laws for borgs on the chosen station
            if (Transform(ent).GridUid != chosenStation)
                continue;

            _siliconLaw.SetLaws(laws, ent, provider.LawUploadSound);
        }
    }
}
