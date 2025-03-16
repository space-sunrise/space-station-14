using System.Diagnostics;
using System.Linq;
using Content.Server._Sunrise.BloodCult.GameRule;
using Content.Server._Sunrise.BloodCult.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;

namespace Content.Server._Sunrise.BloodCult.Objectives.Systems;

public sealed class KillCultistTargetsConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillCultistTargetsConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        SubscribeLocalEvent<KillCultistTargetsConditionComponent, ObjectiveAssignedEvent>(OnPersonAssigned);
        SubscribeLocalEvent<KillCultistTargetsConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnAfterAssign(EntityUid uid,
        KillCultistTargetsConditionComponent comp,
        ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid, GetTitle(comp.Targets, comp.Title), args.Meta);
    }

    private string GetTitle(List<EntityUid> targets, string title)
    {
        var targetsList = "";
        foreach (var target in targets)
        {
            if (!TryComp<MindComponent>(target, out var mind) || mind.CharacterName == null)
                continue;

            var targetName = mind.CharacterName;
            var jobName = _job.MindTryGetJobName(target);
            targetsList += Loc.GetString("objective-condition-cult-kill-target",
                ("targetName", targetName),
                ("job", jobName));
            targetsList += "\n";
        }

        return Loc.GetString(title, ("targets", targetsList));
    }

    private void OnGetProgress(EntityUid uid,
        KillCultistTargetsConditionComponent comp,
        ref ObjectiveGetProgressEvent args)
    {
        args.Progress = KillCultistTargetsProgress(args.MindId);
    }

    private void OnPersonAssigned(EntityUid uid,
        KillCultistTargetsConditionComponent component,
        ref ObjectiveAssignedEvent args)
    {
        // target already assigned
        if (component.Targets.Count != 0)
            return;

        var cultistRule = EntityManager.EntityQuery<BloodCultRuleComponent>().FirstOrDefault();
        Debug.Assert(cultistRule != null, nameof(cultistRule) + " != null");
        var cultTargets = cultistRule.CultTargets;

        component.Targets = cultTargets;
    }

    private bool GetTagretProgress(EntityUid target)
    {
        // deleted or gibbed or something, counts as dead
        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
            return true;

        // dead is success
        return _mind.IsCharacterDeadIc(mind);
    }

    private float KillCultistTargetsProgress(EntityUid? mindId)
    {
        var cultistRule = EntityManager.EntityQuery<BloodCultRuleComponent>().FirstOrDefault();
        Debug.Assert(cultistRule != null, nameof(cultistRule) + " != null");
        var cultTargets = cultistRule.CultTargets;

        var targetsCount = cultTargets.Count;

        // prevent divide-by-zero
        if (targetsCount == 0)
            return 1f;

        var deadTargetsCount = 0;

        foreach (var cultTarget in cultTargets)
        {
            if (GetTagretProgress(cultTarget))
            {
                deadTargetsCount += 1;
            }
        }

        return deadTargetsCount / (float)targetsCount;
    }
}
