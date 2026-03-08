using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems;

/// <summary>
/// Sunrise — admin smite verb that assigns all Traitor antagonists a kill objective
/// targeting the selected player.
/// </summary>
public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly SharedJobSystem _jobSystem = default!;
    [Dependency] private readonly TargetObjectiveSystem _targetObjective = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    private void AddBountySmiteVerb(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        if (!HasComp<MindContainerComponent>(args.Target))
            return;

        var bountyName = Loc.GetString("admin-smite-traitor-bounty-name");
        Verb bounty = new()
        {
            Text = bountyName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Misc/id_cards.rsi"), "centcom"),
            Act = () =>
            {
                AssignTraitorBounty(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", bountyName, Loc.GetString("admin-smite-traitor-bounty-description")),
        };
        args.Verbs.Add(bounty);
    }

    private void AssignTraitorBounty(EntityUid target)
    {
        if (!_mindSystem.TryGetMind(target, out var targetMindId, out var targetMind))
            return;

        var targetName = targetMind.CharacterName ?? "Unknown";
        var jobName = _jobSystem.MindTryGetJobName(targetMindId);

        var traitorCount = 0;
        var query = EntityQueryEnumerator<TraitorRuleComponent>();

        while (query.MoveNext(out var ruleUid, out _))
        {
            foreach (var mind in _antag.GetAntagMinds(ruleUid))
            {
                // Don't assign a kill objective targeting themselves
                if (mind.Owner == targetMindId)
                    continue;

                // Spawn the objective entity
                var objectiveUid = Spawn("AdminBountyKillObjective");

                // Set the target on the objective
                _targetObjective.SetTarget(objectiveUid, targetMindId);

                // Set the entity name (title) for the objective
                var title = Loc.GetString("objective-condition-admin-bounty-kill-title",
                    ("targetName", targetName),
                    ("job", jobName));
                _metaSystem.SetEntityName(objectiveUid, title);

                // Add the objective to the traitor's mind
                _mindSystem.AddObjective(mind.Owner, mind.Comp, objectiveUid);

                // Notify the traitor if they have a session
                if (mind.Comp.UserId is { } userId &&
                    _playerManager.TryGetSessionById(userId, out var session))
                {
                    _chatManager.DispatchServerMessage(session,
                        Loc.GetString("admin-bounty-card-new-objective",
                            ("targetName", targetName),
                            ("job", jobName)));
                }

                traitorCount++;
            }
        }
    }
}
