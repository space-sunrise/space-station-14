using Content.Server._Sunrise.TraitorTarget;
using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Configuration;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles kill person condition logic and picking random kill targets.
/// </summary>
public sealed class KillPersonConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillPersonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, KillPersonConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value, comp.RequireDead, comp.RequireMaroon);
    }

    private float GetProgress(EntityUid target, bool requireDead, bool requireMaroon)
    {
        // deleted or gibbed or something, counts as dead
        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
            return 1f;

        var targetDead = _mind.IsCharacterDeadIc(mind);
        var targetMarooned = !_emergencyShuttle.IsTargetEscaping(target) &&
                              _emergencyShuttle.ShuttlesLeft;
        if (!_config.GetCVar(CCVars.EmergencyShuttleEnabled) && requireMaroon)
        {
            requireDead = true;
            requireMaroon = false;
        }

        if (requireDead && !targetDead)
            return 0f;

        if (requireMaroon) // if evac is still here and target hasn't boarded, show 50% to give you an indicator that you are doing good
            return targetMarooned ? 1f : _emergencyShuttle.EmergencyShuttleArrived ? 0.5f : 0f;

        return 1f; // Good job you did it woohoo
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
