using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.FleshCult;

public sealed partial class CauseFleshCultInfectionEntityEffectSystem : EntityEffectSystem<MobStateComponent, CauseFleshCultInfection>
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    protected override void Effect(Entity<MobStateComponent> entity, ref EntityEffectEvent<CauseFleshCultInfection> args)
    {
        // Fish-start
        if (_entityManager.HasComponent<MindShieldComponent>(entity))
        {
            // Inject into chemical solution (bloodstream) specifically, like hyposprays/syringes do
            if (_entityManager.TryGetComponent<BloodstreamComponent>(entity, out var bloodstream))
            {
                var solutionContainerSystem = _entityManager.System<SharedSolutionContainerSystem>();
                if (solutionContainerSystem.ResolveSolution(entity.Owner, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var chemSolution))
                {
                    // Remove Carol reagent and replace with Unstable Mutagen
                    chemSolution.RemoveReagent("Carol", FixedPoint2.New(5));
                    chemSolution.AddReagent("UnstableMutagen", FixedPoint2.New(5));
                }
            }
        }
        else
        {
            _entityManager.EnsureComponent<PendingFleshCultistComponent>(entity.Owner);
        }
        // Fish-end
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
