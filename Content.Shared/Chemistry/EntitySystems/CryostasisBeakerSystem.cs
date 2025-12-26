using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components.SolutionManager;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// System that prevents solutions in cryostasis beakers from being heated above room temperature.
/// </summary>
public sealed class CryostasisBeakerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SolutionComponent, SolutionChangedEvent>(OnSolutionChanged);
    }

    private void OnSolutionChanged(EntityUid solutionEntity, SolutionComponent solutionComp, ref SolutionChangedEvent args)
    {
        // Get the container that holds this solution
        if (!TryComp<ContainedSolutionComponent>(solutionEntity, out var containedSolution))
            return;

        var containerEntity = containedSolution.Container;
        
        // Check if the container is a CryostasisBeaker
        if (!TryComp<CryostasisBeakerComponent>(containerEntity, out var cryostasisBeaker))
            return;

        var solution = solutionComp.Solution;
        
        // If the solution temperature is above the maximum allowed, cool it down
        if (solution.Temperature > cryostasisBeaker.MaxTemperature)
        {
            solution.Temperature = cryostasisBeaker.MaxTemperature;
        }
    }
}