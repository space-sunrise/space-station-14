using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Modsuit;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
//[Access(typeof(SharedModsuitSystem))]
public sealed partial class ModsuitComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isActivated"), AutoNetworkedField]
    public bool IsActivated = false;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("roundStartBiocode"), AutoNetworkedField]
    public bool RoundStartBiocode = false;
}

