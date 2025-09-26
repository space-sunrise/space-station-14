using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Medical.PsychologistSystem;

[RegisterComponent, NetworkedComponent]
public sealed partial class SolutionIngestBlockerComponent : Component
{
    [DataField]
    public ProtoId<ReagentPrototype> ReagentForBlock = "Ethanol";
}

[RegisterComponent]
public sealed partial class PsychologistBlockAlcoholComponent : Component
{
}
