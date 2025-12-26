using Content.Shared._Sunrise.SolutionRegenerationSwitcher;
using Content.Shared.Chemistry.Reagent;

namespace Content.Client._Sunrise.SolutionRegenerationSwitcher;

public sealed class SolutionRegenerationSwitcherSystem : SharedSolutionRegenerationSwitcherSystem
{
    protected override void SwitchToNextReagent(EntityUid uid,
        SolutionRegenerationSwitcherComponent component,
        EntityUid user)
    {
    }

    protected override void SwitchReagent(EntityUid uid,
        ReagentQuantity reagent,
        SolutionRegenerationSwitcherComponent component,
        EntityUid user)
    {
    }
}
