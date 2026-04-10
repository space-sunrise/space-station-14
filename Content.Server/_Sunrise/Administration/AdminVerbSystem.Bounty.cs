using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives;
using Content.Server.Objectives.Components;
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
    [Dependency] private readonly ObjectivesSystem _objectivesSystem = default!;

    private const string AdminBountyKillObjectiveProto = "AdminBountyKillObjective";

    private static readonly SpriteSpecifier BountyVerbIcon =
        new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/job_icons.rsi"), "Syndicate");

    private void AddBountySmiteVerb(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        var bountyName = Loc.GetString("admin-smite-traitor-bounty-name");
        var target = args.Target;
        Verb bounty = new()
        {
            Text = bountyName,
            Category = VerbCategory.Smite,
            Icon = BountyVerbIcon,
            Act = () =>
            {
                AssignTraitorBounty(target, player);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", bountyName, Loc.GetString("admin-smite-traitor-bounty-description")),
        };
        args.Verbs.Add(bounty);
    }

    private void AssignTraitorBounty(EntityUid target, ICommonSession admin)
    {
        if (!_mindSystem.TryGetMind(target, out var targetMindId, out var targetMind))
        {
            _mindSystem.MakeSentient(target);
            var newMind = _mindSystem.CreateMind(null, Name(target));
            _mindSystem.TransferTo(newMind, target);
            targetMindId = newMind;
            targetMind = Comp<MindComponent>(newMind);
        }

        var targetName = targetMind.CharacterName ?? Name(target);
        var jobName = _jobSystem.MindTryGetJobName(targetMindId);

        var traitorCount = 0;
        var query = EntityQueryEnumerator<TraitorRuleComponent>();
        var antagQuery = GetEntityQuery<AntagSelectionComponent>();

        while (query.MoveNext(out var ruleUid, out _))
        {
            if (!antagQuery.HasComponent(ruleUid))
                continue;

            foreach (var mind in _antag.GetAntagMinds(ruleUid))
            {
                // Don't assign a kill objective targeting themselves
                if (mind.Owner == targetMindId)
                    continue;

                // Skip if this traitor already has a kill objective on the same target
                if (HasKillObjectiveOn(mind.Comp, targetMindId))
                    continue;

                // Create the objective through the objective system (validates ObjectiveComponent, runs assignment hooks)
                if (_objectivesSystem.TryCreateObjective(mind.Owner, mind.Comp, AdminBountyKillObjectiveProto) is not { } objectiveUid)
                    continue;

                // Set the target on the objective (must be done after creation since it's a runtime value)
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

        _chatManager.DispatchServerMessage(admin,
            Loc.GetString("admin-bounty-card-result",
                ("count", traitorCount),
                ("targetName", targetName)));
    }

    /// <summary>
    /// Checks if the mind already has a KillPersonCondition objective targeting the given mind.
    /// </summary>
    private bool HasKillObjectiveOn(MindComponent mind, EntityUid targetMindId)
    {
        foreach (var objective in mind.Objectives)
        {
            if (!TryComp<KillPersonConditionComponent>(objective, out _))
                continue;

            if (!TryComp<TargetObjectiveComponent>(objective, out var targetObj))
                continue;

            if (targetObj.Target == targetMindId)
                return true;
        }

        return false;
    }
}
