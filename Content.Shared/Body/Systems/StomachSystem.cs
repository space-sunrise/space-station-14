using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Utility;
using Content.Shared.Chemistry.Reagent;
using System.Linq;

namespace Content.Shared.Body.Systems;

public sealed class StomachSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public const string DefaultSolutionName = "stomach";

    public bool CanTransferSolution(
        EntityUid uid,
        Solution solution,
        StomachComponent? stomach = null,
        SolutionContainerManagerComponent? solutions = null)
    {
        return Resolve(uid, ref stomach, ref solutions, logMissing: false)
            && _solutionContainerSystem.ResolveSolution((uid, solutions), DefaultSolutionName, ref stomach.Solution, out var stomachSolution)
            // TODO: For now no partial transfers. Potentially change by design
            && stomachSolution.CanAddSolution(solution);
    }

    public bool TryTransferSolution(
        EntityUid uid,
        Solution solution,
        StomachComponent? stomach = null,
        SolutionContainerManagerComponent? solutions = null)
    {
        if (!Resolve(uid, ref stomach, ref solutions, logMissing: false)
            || !_solutionContainerSystem.ResolveSolution((uid, solutions), DefaultSolutionName, ref stomach.Solution)
            || !CanTransferSolution(uid, solution, stomach, solutions))
        {
            return false;
        }

        _solutionContainerSystem.TryAddSolution(stomach.Solution.Value, solution);

        return true;
    }
    // Sunrise-Start
    public bool TryChangeReagent(EntityUid uid,
        string fromReagent,
        string toReagent,
        StomachComponent? stomach = null,
        SolutionContainerManagerComponent? solutions = null)
    {
        if (!Resolve(uid, ref stomach, ref solutions, false))
            return false;

        if (!_solutionContainerSystem.ResolveSolution((uid, solutions), DefaultSolutionName, ref stomach.Solution))
            return false;

        foreach (var reagent in stomach.Solution.Value.Comp.Solution.Contents.ToList())
        {
            if (reagent.Reagent.Prototype != fromReagent)
                continue;

            var amount = reagent.Quantity;

            stomach.Solution.Value.Comp.Solution.RemoveReagent(reagent.Reagent.Prototype, amount);
            foreach (var stomachReagentDelta in stomach.ReagentDeltas.ToList())
            {
                if (stomachReagentDelta.ReagentQuantity.Reagent.Prototype != reagent.Reagent.Prototype)
                    continue;

                stomach.ReagentDeltas.Remove(stomachReagentDelta);
                var newDelta = new StomachComponent.ReagentDelta(new ReagentQuantity(
                    new ReagentId(toReagent, stomachReagentDelta.ReagentQuantity.Reagent.Data),
                    stomachReagentDelta.ReagentQuantity.Quantity));
                stomach.ReagentDeltas.Add(newDelta);
            }

            stomach.Solution.Value.Comp.Solution.AddReagent(toReagent, amount);

            return true;
        }

        return false;
    }
    // Sunrise-End
}
