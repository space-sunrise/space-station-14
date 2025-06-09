using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.FleshCult;

public sealed partial class CauseFleshCultInfection : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cause-flesh-cultist-infection", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        entityManager.EnsureComponent<PendingFleshCultistComponent>(args.TargetEntity);
    }
}

