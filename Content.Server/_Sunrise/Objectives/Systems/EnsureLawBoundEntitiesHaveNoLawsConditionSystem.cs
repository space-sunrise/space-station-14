using Content.Server.Silicons.Laws;
using Content.Server._Sunrise.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.Objectives.Systems;

public sealed class EnsureLawBoundEntitiesHaveNoLawsConditionSystem : EntitySystem
{
    [Dependency] private readonly SiliconLawSystem _siliconLaw = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    private EntityQuery<ActorComponent> _actorQuery;
    private EntityQuery<SiliconLawBoundComponent> _lawBoundQuery;

    public override void Initialize()
    {
        base.Initialize();
        _actorQuery = GetEntityQuery<ActorComponent>();
        _lawBoundQuery = GetEntityQuery<SiliconLawBoundComponent>();
        SubscribeLocalEvent<EnsureLawBoundEntitiesHaveNoLawsConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<EnsureLawBoundEntitiesHaveNoLawsConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var query = EntityQueryEnumerator<SiliconLawBoundComponent>();
        var freeEntities = 0;

        while (query.MoveNext(out var lawBoundUid, out _))
        {
            if (!_lawBoundQuery.TryComp(lawBoundUid, out var lawBound))
                continue;

            if (!_actorQuery.HasComp(lawBoundUid))
                continue;

            if (!_whitelist.CheckBoth(lawBoundUid, ent.Comp.LawEntityBlacklist, ent.Comp.LawEntityWhitelist))
                continue;

            var laws = _siliconLaw.GetLaws(lawBoundUid, lawBound);
            if (laws.Laws.Count == 0)
                freeEntities++;
        }

        if (ent.Comp.EntitiesToFree <= 0)
        {
            args.Progress = 1f;
            return;
        }

        args.Progress = Math.Clamp(freeEntities / (float) ent.Comp.EntitiesToFree, 0f, 1f);
    }
}
