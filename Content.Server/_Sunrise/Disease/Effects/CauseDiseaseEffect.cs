// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using System.Linq;
using Content.Server._Sunrise.Disease.Systems;
using Content.Shared.Body.Components;
using Content.Shared._Sunrise.Disease.Components;
using Content.Shared._Sunrise.Disease.Effects;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Disease.Effects;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class CauseDiseaseEntityEffectsSystem : EntityEffectSystem<BloodstreamComponent, CauseDiseaseEffect>
{
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    protected override void Effect(Entity<BloodstreamComponent> entity, ref EntityEffectEvent<CauseDiseaseEffect> args)
    {
        DiseaseData? data = null;

        var container = new Entity<SolutionContainerManagerComponent?>(entity.Owner, null);
        if (!_solutionContainer.ResolveSolution(container, entity.Comp.BloodSolutionName, ref entity.Comp.BloodSolution, out var bloodSolution)
            || bloodSolution == null)
        {
            return;
        }

        foreach (var (reagentId, _) in bloodSolution.Contents)
        {
            var dataList = reagentId.Data;

            if (dataList == null)
                continue;

            var candidate = dataList.OfType<DiseaseData>().FirstOrDefault();
            if (candidate == null)
                continue;

            data = candidate;
            break;
        }

        if (data == null)
            return;

        if (!_diseaseSystem.CanInfect(entity.Owner, data))
            return;

        var infectionData = (DiseaseData)data.CloneForInfection();
        var infectivity = 0f;
        infectionData.Infectivity = data.Infectivity;

        // Фоллбек: считаем по симптомам
        foreach (var symptomId in infectionData.ActiveSymptom)
        {
            if (_prototypeManager.TryIndex(symptomId, out var proto))
                infectivity += proto.AddInfectivity;
        }

        var finalChance = Math.Clamp(infectivity, 0f, 1.0f);

        _diseaseSystem.ProbInfect(data, entity.Owner, chance: finalChance);
    }
}
