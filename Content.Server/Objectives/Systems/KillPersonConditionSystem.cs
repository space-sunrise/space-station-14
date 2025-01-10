using Content.Server._Sunrise.TraitorTarget;
using Content.Server.Objectives.Components;
using Content.Server.Revolutionary.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles kill person condition logic and picking random kill targets.
/// </summary>
public sealed class KillPersonConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillPersonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        SubscribeLocalEvent<PickRandomPersonComponent, ObjectiveAssignedEvent>(OnPersonAssigned);

        SubscribeLocalEvent<PickRandomHeadComponent, ObjectiveAssignedEvent>(OnHeadAssigned);

        SubscribeLocalEvent<PickRandomAntagComponent, ObjectiveAssignedEvent>(OnAntagAssigned);
    }

    private void OnGetProgress(EntityUid uid, KillPersonConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value, comp.RequireDead);
    }

    private void OnPersonAssigned(EntityUid uid, PickRandomPersonComponent comp, ref ObjectiveAssignedEvent args)
    {
        // invalid objective prototype
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        var allHumans = GetAliveTargetsExcept(args.MindId); // Sunrise-Edit

        // Can't have multiple objectives to kill the same person
        foreach (var objective in args.Mind.Objectives)
        {
            if (HasComp<KillPersonConditionComponent>(objective) && TryComp<TargetObjectiveComponent>(objective, out var kill))
            {
                allHumans.RemoveWhere(x => x.Owner == kill.Target);
            }
        }

        // no other humans to kill
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        if (TryComp<MindComponent>(args.MindId, out var mindComponent) &&
            TryComp<AntagTargetComponent>(mindComponent.OwnedEntity, out var antagTargetCom))
        {
            antagTargetCom.KillerMind = args.MindId;
        }

        _target.SetTarget(uid, _random.Pick(allHumans), target);
    }

    private void OnHeadAssigned(EntityUid uid, PickRandomHeadComponent comp, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        // no other humans to kill
        var allHumans = GetAliveTargetsExcept(args.MindId);
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        var allHeads = new HashSet<Entity<MindComponent>>();
        foreach (var person in allHumans)
        {
            if (TryComp<MindComponent>(person, out var mind) && mind.OwnedEntity is { } ent && HasComp<CommandStaffComponent>(ent))
                allHeads.Add(person);
        }

        if (allHeads.Count == 0)
            allHeads = allHumans; // fallback to non-head target

        if (TryComp<MindComponent>(args.MindId, out var mindComponent) &&
            TryComp<AntagTargetComponent>(mindComponent.OwnedEntity, out var antagTargetCom))
        {
            antagTargetCom.KillerMind = args.MindId;
        }

        _target.SetTarget(uid, _random.Pick(allHeads), target);
    }

    private void OnAntagAssigned(EntityUid uid, PickRandomAntagComponent comp, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
        {
            args.Cancelled = true;
            return;
        }

        if (target.Target != null)
            return;

        var allHumans = GetAliveTargetsExcept(args.MindId);
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        var allAntags = new HashSet<Entity<MindComponent>>();
        foreach (var person in allHumans)
        {
            if (_roleSystem.MindIsAntagonist(person))
                allAntags.Add(person);
        }

        if (allAntags.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        if (TryComp<MindComponent>(args.MindId, out var mindComponent) &&
            TryComp<AntagTargetComponent>(mindComponent.OwnedEntity, out var antagTargetCom))
        {
            antagTargetCom.KillerMind = args.MindId;
        }

        _target.SetTarget(uid, _random.Pick(allAntags), target);
    }

    private float GetProgress(EntityUid target, bool requireDead)
    {
        // deleted or gibbed or something, counts as dead
        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
            return 1f;

        // dead is success
        if (_mind.IsCharacterDeadIc(mind))
            return 1f;

        // if the target has to be dead dead then don't check evac stuff
        if (requireDead)
            return 0f;

        // if evac is disabled then they really do have to be dead
        if (!_config.GetCVar(CCVars.EmergencyShuttleEnabled))
            return 0f;

        // target is escaping so you fail
        if (_emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value))
            return 0f;

        // evac has left without the target, greentext since the target is afk in space with a full oxygen tank and coordinates off.
        if (_emergencyShuttle.ShuttlesLeft)
            return 1f;

        // if evac is still here and target hasn't boarded, show 50% to give you an indicator that you are doing good
        return _emergencyShuttle.EmergencyShuttleArrived ? 0.5f : 0f;
    }

    // Sunrise-Start
    public HashSet<Entity<MindComponent>> GetAliveTargetsExcept(EntityUid exclude)
    {
        var allTargets = new HashSet<Entity<MindComponent>>();

        var query = EntityQueryEnumerator<MobStateComponent, AntagTargetComponent, HumanoidAppearanceComponent>();
        while (query.MoveNext(out var uid, out var mobState, out var antagTarget, out _))
        {
            if (!_mind.TryGetMind(uid, out var mind, out var mindComp) ||
                mind == exclude || !_mobState.IsAlive(uid, mobState) ||
                antagTarget.KillerMind != null)
                continue;

            allTargets.Add(new Entity<MindComponent>(mind, mindComp));
        }

        return allTargets;
    }
    // Sunrise-End
}
