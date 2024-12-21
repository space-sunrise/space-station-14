using Content.Server._Sunrise.FleshCult.GameRule;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server._Sunrise.FleshCult.Objectives;

public sealed class FleshCultConditionsSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CreateFleshHeartConditionComponent, ObjectiveGetProgressEvent>(OnFleshHeartGetProgress);
    }

    private void OnFleshHeartGetProgress(EntityUid uid, CreateFleshHeartConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = FleshHeartProgress(args.MindId, _number.GetTarget(uid));
    }

    private float FleshHeartProgress(EntityUid? mindId, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        if (!TryComp<FleshCultistRoleComponent>(mindId, out var role))
            return 0f;

        var query = EntityQueryEnumerator<FleshCultRuleComponent, GameRuleComponent>();

        while (query.MoveNext(out var uid, out var fleshCult, out var gameRule))
        {
            return fleshCult.FleshHeartActive ? 1f : 0f;
        }

        return 0f;
    }
}
