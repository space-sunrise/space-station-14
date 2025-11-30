using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.FleshCult;

public sealed partial class CauseFleshCultInfectionEntityEffectSystem : EntityEffectSystem<MobStateComponent, CauseFleshCultInfection>
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    protected override void Effect(Entity<MobStateComponent> entity, ref EntityEffectEvent<CauseFleshCultInfection> args)
    {
        _entityManager.EnsureComponent<PendingFleshCultistComponent>(entity.Owner);
    }
}

public sealed partial class CauseFleshCultInfection : EntityEffectBase<CauseFleshCultInfection>
{
    [DataField]
    public bool Innoculate;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-cause-flesh-cultist-infection", ("chance", Probability));
    }
}

