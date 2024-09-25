using Content.Server.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Objectives.Systems;

public sealed class EscapeShuttleTeamConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly BloodBrotherRuleSystem _broRule = default!;

    private List<(EntityUid Id, MindComponent Mind)> _teamMembers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EscapeShuttleTeamConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<EscapeShuttleTeamConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
    }

    private void OnGetProgress(EntityUid uid, EscapeShuttleTeamConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (_teamMembers == null || _teamMembers.Count == 0)
            return;

        args.Progress = GetTeamProgress(_teamMembers);
    }

    private void OnAssigned(EntityUid uid, EscapeShuttleTeamConditionComponent comp, ref ObjectiveAssignedEvent args)
    {
        var teamMembers = Enumerable.ToList(_broRule.GetOtherBroMindsAliveAndConnected(args.Mind));

        if (teamMembers.Count == 0)
        {
            args.Cancelled = true;
        }
        
        _teamMembers = teamMembers;
    }

    private float GetTeamProgress(List<(EntityUid Id, MindComponent Mind)> teamMembers)
    {
        int totalMembers = teamMembers.Count;
        int succeededMembers = 0;

        foreach (var (id, mind) in teamMembers)
        {
            if (IsMemberEscaping(mind))
            {
                succeededMembers++;
            }
        }

        return totalMembers > 0 ? (float)succeededMembers / totalMembers : 0f;
    }

    private bool IsMemberEscaping(MindComponent mind)
    {
        if (mind.OwnedEntity == null || _mind.IsCharacterDeadIc(mind))
            return false;

        if (TryComp<CuffableComponent>(mind.OwnedEntity, out var cuffed) && cuffed.CuffedHandCount > 0)
            return false;

        return _emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value);
    }
}
