using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.NPC.Systems;

namespace Content.Server.GameTicking.Rules;

public sealed class TraitorRuleSyndicateFactionSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TraitorRuleComponent, AfterAntagEntitySelectedEvent>(
            OnAfterAntagEntitySelected,
            after: [typeof(TraitorRuleSystem)]);
    }

    private void OnAfterAntagEntitySelected(Entity<TraitorRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        _npcFaction.AddFaction(args.EntityUid, ent.Comp.SyndicateFaction);
    }
}
