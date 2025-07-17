using Content.Server.Silicons.Laws;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Prototypes;
using Content.Server.Silicons.Borgs;
using Content.Server.SyndicateTeleporter;
using Content.Shared.Emag.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.Tag;

namespace Content.Server.StationEvents.Events;

public sealed class EpsilonDeathSquadLawsetRule : StationEventSystem<StationEventComponent>
{
    [Dependency] private readonly SiliconLawSystem _siliconLaw = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private const string DeathSquadLawsetId = "DeathSquadLawset";

    protected override void Started(EntityUid uid, StationEventComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        var lawsetId = DeathSquadLawsetId;
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
            // Skip EMAGed borgs
            if (_emag.CheckFlag(ent, EmagType.Interaction))
                continue;
            // Skip Syndicate borgs
            if (TryComp<NpcFactionMemberComponent>(ent, out var faction) && faction.Factions is {} factions && factions.Contains("Syndicate"))
                continue;
            _siliconLaw.SetLaws(laws, ent, provider.LawUploadSound);
        }
    }
}
