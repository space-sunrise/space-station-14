// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.Disease;
using Content.Shared.EntityEffects;
public sealed partial class CureDiseaseInfection : EntityEffect
{
    [DataField]
    public bool Innoculate;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (Innoculate)
            return "ок";

        return "окей";
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        if (!entityManager.HasComponent<SickComponent>(args.TargetEntity)) return;
        if (entityManager.TryGetComponent<SickComponent>(args.TargetEntity, out var sick))
        {
            if (entityManager.TryGetComponent<DiseaseRoleComponent>(sick.owner, out var disease))
            {
                var comp = entityManager.EnsureComponent<DiseaseVaccineTimerComponent>(args.TargetEntity);
                comp.Immune = Innoculate;
                comp.Delay = TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(disease.Shield * 30);
            }
        }
    }
}
