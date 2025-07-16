using Content.Server.Silicons.Laws;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Events;

public sealed class EpsilonDeathSquadLawsetRule : StationEventSystem<StationEventComponent>
{
    [Dependency] private readonly SiliconLawSystem _siliconLaw = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    protected override void Started(EntityUid uid, StationEventComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        var lawsetId = "DeathSquadLawset";
        if (!_prototypeManager.TryIndex<SiliconLawsetPrototype>(lawsetId, out var lawsetProto))
        {
            Logger.GetSawmill("station-event").Error($"Could not find lawset prototype: {lawsetId}");
            return;
        }

        // Convert the prototype's law IDs to actual law objects
        var laws = new List<SiliconLaw>();
        foreach (var lawId in lawsetProto.Laws)
        {
            if (_prototypeManager.TryIndex<SiliconLawPrototype>(lawId, out var lawProto))
                laws.Add(lawProto);
        }

        var query = EntityQueryEnumerator<SiliconLawProviderComponent>();
        while (query.MoveNext(out var ent, out var provider))
        {
            _siliconLaw.SetLaws(laws, ent, provider.LawUploadSound);
        }
    }
}
