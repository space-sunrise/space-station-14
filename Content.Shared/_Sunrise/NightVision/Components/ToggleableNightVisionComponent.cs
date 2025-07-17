using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.NightVision.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToggleableNightVisionComponent : Component
{
    [DataField]
    public EntProtoId Action = "ToggleableNightVision";

    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "EffectNightVision";

    [ViewVariables]
    public EntityUid? ActionEntity;

    [ViewVariables]
    public bool Active;
}
