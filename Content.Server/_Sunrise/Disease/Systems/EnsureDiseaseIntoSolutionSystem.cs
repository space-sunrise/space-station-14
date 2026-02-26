// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Server._Sunrise.Disease.Components;
using Content.Shared._Sunrise.Disease.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;


namespace Content.Server._Sunrise.Disease.Systems;

public sealed partial class EnsureDiseaseIntoSolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly DiseaseDiagnoserSystem _diseaseDiagnoser = default!;
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        // Solution entities are spawned/ensured on MapInit in SharedSolutionContainerSystem.
        SubscribeLocalEvent<EnsureDiseaseIntoSolutionComponent, MapInitEvent>(OnMapInit, after: [typeof(SharedSolutionContainerSystem)]);
        SubscribeLocalEvent<EnsureDiseaseIntoSolutionComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnMapInit(Entity<EnsureDiseaseIntoSolutionComponent> ent, ref MapInitEvent args)
    {
        Apply(ent);
    }

    private void OnComponentStartup(EntityUid uid, EnsureDiseaseIntoSolutionComponent component, ComponentStartup args)
    {
        Apply((uid, component));
    }

    private void Apply(Entity<EnsureDiseaseIntoSolutionComponent> ent)
    {
        if (!TryComp<SolutionContainerManagerComponent>(ent.Owner, out var solutionContainerManager))
            return;

        if (!TryComp<DrawableSolutionComponent>(ent.Owner, out var drawable))
            return;

        var entWrapper = new Entity<DrawableSolutionComponent?, SolutionContainerManagerComponent?>(ent.Owner, drawable, solutionContainerManager);

        if (!_solutionContainer.TryGetDrawableSolution(entWrapper, out Entity<SolutionComponent>? solutionEntity, out Solution? solution))
            return;

        if (solutionEntity == null || solution == null)
            return;

        _solutionContainer.TryAddReagent(solutionEntity.Value, _diseaseDiagnoser.Reagent, solution.MaxVolume, out _);

        for (var i = 0; i < solution.Contents.Count; i++)
        {
            var reagent = solution.Contents[i];

            if (reagent.Reagent.Prototype != _diseaseDiagnoser.Reagent)
                continue;

            var reagentData = reagent.Reagent.Data != null
                ? new List<ReagentData>(reagent.Reagent.Data)
                : new List<ReagentData>();

            reagentData.RemoveAll(x => x is DiseaseData);

            var diseaseData = (DiseaseData)(ent.Comp.Data?.Clone() ?? _diseaseSystem.CreateNewDisease());
            reagentData.Add(diseaseData);

            solution.Contents[i] = new ReagentQuantity(new ReagentId(_diseaseDiagnoser.Reagent, reagentData), reagent.Quantity);
        }

        _solutionContainer.UpdateChemicals(solutionEntity.Value);
    }


}
