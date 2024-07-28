using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Sunrise.SolutionRegenerationSwitcher
{
    [RegisterComponent]
    public sealed partial class SolutionRegenerationSwitcherComponent : Component
    {
        [DataField("options", required: true), ViewVariables(VVAccess.ReadWrite)]
        public List<ReagentQuantity> Options = default!;

        [DataField("currentIndex"), ViewVariables(VVAccess.ReadWrite)]
        public int CurrentIndex = 0;

        /// <summary>
        /// Should the already generated solution be kept when switching?
        /// </summary>
        [DataField("keepSolution"), ViewVariables(VVAccess.ReadWrite)]
        public bool KeepSolution = false;
    }
}
