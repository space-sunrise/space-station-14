using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.ThermalVision;


[RegisterComponent, NetworkedComponent]
public sealed partial class ToggleableThermalVisionComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionToggleThermalVision";

    [ViewVariables]
    public EntityUid? ActionEntity;

    [ViewVariables]
    public bool Active;
}
