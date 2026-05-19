using Content.Server.Antag;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Database;
using Content.Shared.Paper;
using Content.Shared.Whitelist;
using Content.Shared._Starlight.Paper;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Starlight.Paper;

public sealed class AntagOnSignSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    private readonly EntProtoId _paradoxCloneRuleId = "ParadoxCloneSpawn";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagOnSignComponent, PaperSignedEvent>(OnPaperSigned);
        SubscribeLocalEvent<AntagOnSignComponent, PaperWriteEvent>(OnPaperSigned, before: [typeof(ObjectiveOnSignSystem)]);
    }

    private void OnPaperSigned(EntityUid uid, AntagOnSignComponent component, PaperWriteEvent args)
    {
        if (component.Blacklist != null && _whitelist.IsWhitelistPass(component.Blacklist, args.User))
            return;

        if (component.ChargesRemaining <= 0)
            return;

        var signer = args.User;
        if (!TryComp<ActorComponent>(signer, out var actor))
            return;

        if (component.SignedEntityUids.Contains(signer))
            return;

        component.ChargesRemaining--;
        component.SignedEntityUids.Add(signer);
        _adminLogger.Add(LogType.AntagSelection, LogImpact.Medium,
            $"{ToPrettyString(signer):player} activated antag leaflet {ToPrettyString(uid):entity} (source: write, chance: {component.Chance:0.##}, charges left: {component.ChargesRemaining}).");

        if (_random.NextFloat() > component.Chance)
        {
            _adminLogger.Add(LogType.AntagSelection, LogImpact.Low,
                $"Antag leaflet activation failed chance roll for {ToPrettyString(signer):player} on {ToPrettyString(uid):entity}.");
            return;
        }

        var session = actor.PlayerSession;

        foreach (var antag in component.Antags)
        {
            var targetComp = _componentFactory.GetComponent(antag.TargetComponent);

            var forceMakeAntag = typeof(AntagSelectionSystem).GetMethod(nameof(AntagSelectionSystem.ForceMakeAntag));
            if (forceMakeAntag == null)
            {
                Log.Error("Failed to reflect ForceMakeAntag from AntagSelectionSystem.");
                continue;
            }

            var generic = forceMakeAntag.MakeGenericMethod(targetComp.GetType());
            generic.Invoke(_antag, [session, antag.Antag.Id]);
        }

        _adminLogger.Add(LogType.AntagSelection, LogImpact.High,
            $"{ToPrettyString(signer):player} received antag role(s) from leaflet {ToPrettyString(uid):entity}.");

        if (!component.ParadoxClone)
            return;

        var ruleEnt = _gameTicker.AddGameRule(_paradoxCloneRuleId);

        if (!TryComp<ParadoxCloneRuleComponent>(ruleEnt, out var paradoxCloneRuleComp))
            return;

        paradoxCloneRuleComp.OriginalBody = args.User;
        _gameTicker.StartGameRule(ruleEnt);
    }

    private void OnPaperSigned(EntityUid uid, AntagOnSignComponent component, PaperSignedEvent args)
    {
        if (component.Blacklist != null && _whitelist.IsWhitelistPass(component.Blacklist, args.Signer))
            return;

        if (component.ChargesRemaining <= 0)
            return;

        var signer = args.Signer;
        if (!TryComp<ActorComponent>(signer, out var actor))
            return;

        if (component.SignedEntityUids.Contains(signer))
            return;

        component.ChargesRemaining--;
        component.SignedEntityUids.Add(signer);
        _adminLogger.Add(LogType.AntagSelection, LogImpact.Medium,
            $"{ToPrettyString(signer):player} activated antag leaflet {ToPrettyString(uid):entity} (source: sign, chance: {component.Chance:0.##}, charges left: {component.ChargesRemaining}).");

        if (_random.NextFloat() > component.Chance)
        {
            _adminLogger.Add(LogType.AntagSelection, LogImpact.Low,
                $"Antag leaflet activation failed chance roll for {ToPrettyString(signer):player} on {ToPrettyString(uid):entity}.");
            return;
        }

        var session = actor.PlayerSession;

        foreach (var antag in component.Antags)
        {
            var targetComp = _componentFactory.GetComponent(antag.TargetComponent);

            var forceMakeAntag = typeof(AntagSelectionSystem).GetMethod(nameof(AntagSelectionSystem.ForceMakeAntag));
            if (forceMakeAntag == null)
            {
                Log.Error("Failed to reflect ForceMakeAntag from AntagSelectionSystem.");
                continue;
            }

            var generic = forceMakeAntag.MakeGenericMethod(targetComp.GetType());
            generic.Invoke(_antag, [session, antag.Antag.Id]);
        }

        _adminLogger.Add(LogType.AntagSelection, LogImpact.High,
            $"{ToPrettyString(signer):player} received antag role(s) from leaflet {ToPrettyString(uid):entity}.");

        if (!component.ParadoxClone)
            return;

        var ruleEnt = _gameTicker.AddGameRule(_paradoxCloneRuleId);

        if (!TryComp<ParadoxCloneRuleComponent>(ruleEnt, out var paradoxCloneRuleComp))
            return;

        paradoxCloneRuleComp.OriginalBody = args.Signer;
        _gameTicker.StartGameRule(ruleEnt);
    }
}
