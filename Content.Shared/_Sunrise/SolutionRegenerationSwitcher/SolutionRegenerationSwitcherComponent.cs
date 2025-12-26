using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.SolutionRegenerationSwitcher
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class SolutionRegenerationSwitcherComponent : Component
    {
        [DataField(required: true), ViewVariables, AutoNetworkedField]
        public List<ReagentQuantity> Options = [];

        [AutoNetworkedField]
        public int CurrentIndex;

        [DataField, ViewVariables]
        public bool KeepSolution;
    }
}
