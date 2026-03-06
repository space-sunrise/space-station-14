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

namespace Content.Server._Sunrise.Disease.Effects;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class CauseDiseaseEntityEffectsSystem : EntityEffectSystem<BloodstreamComponent, CauseDiseaseEffect>
{
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
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
        var infectivity = _diseaseSystem.CalcInfectionInfectivity(infectionData);

        // вот не понятно, может же быть реаетивный реагент, тогда резисты нужно учитывать?
        _diseaseSystem.ProbInfect(data, entity.Owner, infectivity: infectivity, ignoreResistance: true);
    }
}
