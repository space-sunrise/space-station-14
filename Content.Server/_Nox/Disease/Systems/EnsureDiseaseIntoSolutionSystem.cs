// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Server._Nox.Disease.Components;
using Content.Shared._Nox.Disease.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;


namespace Content.Server._Nox.Disease.Systems;

public sealed partial class EnsureDiseaseIntoSolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly DiseaseDiagnoserSystem _diseaseDiagnoser = default!;
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnsureDiseaseIntoSolutionComponent, ComponentStartup>(OnComponentStartup, after: new[] { typeof(SharedSolutionContainerSystem) });
    }

    private void OnComponentStartup(EntityUid uid, EnsureDiseaseIntoSolutionComponent component, ComponentStartup args)
    {
        if (!TryComp<SolutionContainerManagerComponent>(uid, out var solutionContainerManager))
            return;

        if (!TryComp<DrawableSolutionComponent>(uid, out var injectable))
            return;

        var entWrapper = new Entity<DrawableSolutionComponent?, SolutionContainerManagerComponent?>(uid, injectable, solutionContainerManager);

        if (!_solutionContainer.TryGetDrawableSolution(entWrapper, out Entity<SolutionComponent>? solutionEntity, out Solution? solution))
            return;

        if (solutionEntity != null && solution != null)
        {
            _solutionContainer.TryAddReagent(solutionEntity.Value, _diseaseDiagnoser.Reagent, solution.MaxVolume, out _);

            foreach (var reagent in solution.Contents)
            {
                if (reagent.Reagent.Prototype != _diseaseDiagnoser.Reagent)
                    return;

                List<ReagentData> reagentData = reagent.Reagent.EnsureReagentData();

                reagentData.RemoveAll(x => x is DiseaseData);

                reagentData.Add(component.Data ?? _diseaseSystem.CreateNewDisease());
            }
        }
    }


}
