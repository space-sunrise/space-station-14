// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Disease;

public sealed partial class CureDiseaseInfectionEntityEffectSystem : EntityEffectSystem<SickComponent, CureDiseaseInfection>
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    protected override void Effect(Entity<SickComponent> entity, ref EntityEffectEvent<CureDiseaseInfection> args)
    {
        if (_entityManager.TryGetComponent<DiseaseRoleComponent>(entity.Owner, out var disease))
        {
            var comp = _entityManager.EnsureComponent<DiseaseVaccineTimerComponent>(entity.Owner);
            comp.Immune = args.Effect.Innoculate;
            comp.Delay = TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(disease.Shield * 30);
        }
    }
}

public sealed partial class CureDiseaseInfection : EntityEffectBase<CureDiseaseInfection>
{
    [DataField]
    public bool Innoculate;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("entity-effect-guidebook-cure-zombie-infection", ("chance", Probability));
    }
}
