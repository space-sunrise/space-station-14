using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Server.Vampire;
using Content.Shared.Vampire.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using System.Text;

namespace Content.Server.GameTicking.Rules;

public sealed partial class VampireRuleSystem : GameRuleSystem<VampireRuleComponent>
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly ObjectivesSystem _objective = default!;
    [Dependency] private readonly VampireSystem _vampire = default!;

    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/Ambience/Antag/vampire_start.ogg");

    public readonly ProtoId<AntagPrototype> VampirePrototypeId = "Vampire";

//    public readonly ProtoId<NpcFactionPrototype> ChangelingFactionId = "Changeling";

//    public readonly ProtoId<NpcFactionPrototype> NanotrasenFactionId = "NanoTrasen";

    public readonly ProtoId<CurrencyPrototype> Currency = "BloodEssence";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireRuleComponent, AfterAntagEntitySelectedEvent>(OnSelectAntag);
        //SubscribeLocalEvent<VampireRuleComponent, ObjectivesTextPrependEvent>(OnTextPrepend);
    }

    private void OnSelectAntag(EntityUid uid, VampireRuleComponent comp, ref AfterAntagEntitySelectedEvent args)
    {
        MakeVampire(args.EntityUid, comp);
    }
    public bool MakeVampire(EntityUid target, VampireRuleComponent rule)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;

        // briefing
        if (TryComp<MetaDataComponent>(target, out var metaData))
        {
            var briefing = Loc.GetString("vampire-role-greeting", ("name", metaData?.EntityName ?? "Unknown"));
            var briefingShort = Loc.GetString("vampire-role-greeting-short", ("name", metaData?.EntityName ?? "Unknown"));

            _antag.SendBriefing(target, briefing, Color.Yellow, BriefingSound);
            _role.MindHasRole<VampireRoleComponent>(mindId, out var vampireRole);
            if (vampireRole is not null)
            {
                AddComp<RoleBriefingComponent>(vampireRole.Value.Owner);
                Comp<RoleBriefingComponent>(vampireRole.Value.Owner).Briefing = briefing;
            }
        }
        // vampire stuff
//        _npcFaction.RemoveFaction(target, NanotrasenFactionId, false);
//        _npcFaction.AddFaction(target, ChangelingFactionId);

        // make sure it's initial chems are set to max
        var vampireComponent = EnsureComp<VampireComponent>(target);

        vampireComponent.Balance = new() { { VampireComponent.CurrencyProto, 0 } };

        rule.VampireMinds.Add(mindId);
        
        if (HasComp<VampireComponent>(target))
        {
            _vampire.AddStartingAbilities(target);
        }

        //foreach (var objective in rule.Objectives)
        //    _mind.TryAddObjective(mindId, mind, objective);

        return true;
    }

/*    private void OnTextPrepend(EntityUid uid, VampireRuleComponent comp, ref ObjectivesTextPrependEvent args)
    {
        var mostAbsorbedName = string.Empty;
        var mostStolenName = string.Empty;
        var mostAbsorbed = 0f;
        var mostStolen = 0f;

        foreach (var ling in EntityQuery<ChangelingComponent>())
        {
            if (!_mind.TryGetMind(ling.Owner, out var mindId, out var mind))
                continue;

            if (!TryComp<MetaDataComponent>(ling.Owner, out var metaData))
                continue;

            if (ling.TotalAbsorbedEntities > mostAbsorbed)
            {
                mostAbsorbed = ling.TotalAbsorbedEntities;
                mostAbsorbedName = _objective.GetTitle((mindId, mind), metaData.EntityName);
            }
            if (ling.TotalStolenDNA > mostStolen)
            {
                mostStolen = ling.TotalStolenDNA;
                mostStolenName = _objective.GetTitle((mindId, mind), metaData.EntityName);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine(Loc.GetString($"roundend-prepend-changeling-absorbed{(!string.IsNullOrWhiteSpace(mostAbsorbedName) ? "-named" : "")}", ("name", mostAbsorbedName), ("number", mostAbsorbed)));
        sb.AppendLine(Loc.GetString($"roundend-prepend-changeling-stolen{(!string.IsNullOrWhiteSpace(mostStolenName) ? "-named" : "")}", ("name", mostStolenName), ("number", mostStolen)));

        args.Text = sb.ToString();
    }*/
}
