using Content.Shared.Chemistry.Reagent;

namespace Content.Server._Sunrise.SolutionRegenerationSwitcher
{
    [RegisterComponent]
    public sealed partial class SolutionRegenerationSwitcherComponent : Component
    {
        [DataField("options", required: true), ViewVariables(VVAccess.ReadWrite)]
        public List<ReagentQuantity> Options = default!;

        [DataField("keepSolution"), ViewVariables(VVAccess.ReadWrite)]
        public bool KeepSolution;
    }
}
