using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Paper;
using Content.Shared._Sunrise.Paper;
using Content.Shared.Whitelist;
using Robust.Shared.Random;
using Content.Shared.Mind;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.Paper;

public sealed class ObjectiveOnSignSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ObjectiveOnSignComponent, PaperSignedEvent>(OnPaperSigned);
        SubscribeLocalEvent<ObjectiveOnSignComponent, PaperWriteEvent>(OnPaperSigned);
    }


    private void OnPaperSigned(EntityUid uid, ObjectiveOnSignComponent component, PaperWriteEvent args)
    {
        if (component.ChargesRemaining <= 0)
            return; // no charges left. dont run the component

        var signer = args.User;

        if (!_whitelistSystem.CheckBoth(signer, component.Blacklist, component.Whitelist))
            return;

        if (!TryComp<ActorComponent>(signer, out var actor))
            return;

        if (component.SignedEntityUids.Contains(signer))
            return;

        if (!_mind.TryGetMind(actor.PlayerSession.UserId, out var mindId, out var mind))
        {
            Log.Error($"Player {ToPrettyString(signer):player} signed a paper with ObjectiveOnSign but had no mind attached!");
            return;
        }

        component.ChargesRemaining--;
        component.SignedEntityUids.Add(signer);
        _adminLogger.Add(LogType.AntagSelection, LogImpact.Medium,
            $"{ToPrettyString(signer):player} activated objective leaflet {ToPrettyString(uid):entity} (source: write, chance: {component.Chance:0.##}, charges left: {component.ChargesRemaining}).");

        if (_random.NextFloat() > component.Chance)
        {
            _adminLogger.Add(LogType.AntagSelection, LogImpact.Low,
                $"Objective leaflet activation failed chance roll for {ToPrettyString(signer):player} on {ToPrettyString(uid):entity}.");
            return; //vibe check failed no events for you.
        }

        if (!component.Append)
        {
            while (_mind.TryRemoveObjective(mindId.Value, mind, 0))
            {
            } // basically just keep trying to remove the 0th objective until there is none
        }

        foreach (var rule in component.Objectives)
        {
            if (!_mind.TryAddObjective(mindId.Value, mind, rule.Id))
                Log.Warning($"Failed to add objective {rule.Id} to {ToPrettyString(signer):player}.");
        }

        _adminLogger.Add(LogType.AntagSelection, LogImpact.Medium,
            $"{ToPrettyString(signer):player} received objective set from leaflet {ToPrettyString(uid):entity}.");

    }

    private void OnPaperSigned(EntityUid uid, ObjectiveOnSignComponent component, PaperSignedEvent args)
    {
        if (component.ChargesRemaining <= 0)
            return; // no charges left. dont run the component

        var signer = args.Signer;

        if (!_whitelistSystem.CheckBoth(signer, component.Blacklist, component.Whitelist))
            return;

        if (!TryComp<ActorComponent>(signer, out var actor))
            return;

        if (component.SignedEntityUids.Contains(signer))
            return;

        if (!_mind.TryGetMind(actor.PlayerSession.UserId, out var mindId, out var mind))
        {
            Log.Error($"Player {ToPrettyString(signer):player} signed a paper with ObjectiveOnSign but had no mind attached!");
            return;
        }

        component.ChargesRemaining--;
        component.SignedEntityUids.Add(signer);
        _adminLogger.Add(LogType.AntagSelection, LogImpact.Medium,
            $"{ToPrettyString(signer):player} activated objective leaflet {ToPrettyString(uid):entity} (source: sign, chance: {component.Chance:0.##}, charges left: {component.ChargesRemaining}).");

        if (_random.NextFloat() > component.Chance)
        {
            _adminLogger.Add(LogType.AntagSelection, LogImpact.Low,
                $"Objective leaflet activation failed chance roll for {ToPrettyString(signer):player} on {ToPrettyString(uid):entity}.");
            return; //vibe check failed no events for you.
        }

        if (!component.Append)
        {
            while (_mind.TryRemoveObjective(mindId.Value, mind, 0))
            {
            } // basically just keep trying to remove the 0th objective until there is none
        }

        foreach (var rule in component.Objectives)
        {
            if (!_mind.TryAddObjective(mindId.Value, mind, rule.Id))
                Log.Warning($"Failed to add objective {rule.Id} to {ToPrettyString(signer):player}.");
        }

        _adminLogger.Add(LogType.AntagSelection, LogImpact.Medium,
            $"{ToPrettyString(signer):player} received objective set from leaflet {ToPrettyString(uid):entity}.");
    }
}
