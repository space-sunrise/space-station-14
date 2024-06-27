<<<<<<< HEAD
// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Server.Zombies;
=======
>>>>>>> master
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Content.Shared.Ligyb;
public sealed partial class CureDiseaseInfection : ReagentEffect
{
    [DataField]
    public bool Innoculate;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (Innoculate)
            return "ок";

        return "окей";
    }

    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        if (!entityManager.HasComponent<SickComponent>(args.SolutionEntity)) return;
        if (entityManager.TryGetComponent<SickComponent>(args.SolutionEntity, out var sick))
        {
            if (entityManager.TryGetComponent<DiseaseRoleComponent>(sick.owner, out var disease))
            {
                var comp = entityManager.EnsureComponent<DiseaseVaccineTimerComponent>(args.SolutionEntity);
                comp.Immune = Innoculate;
                comp.Delay = TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(disease.Shield * 30);
            }
        }
    }
}
