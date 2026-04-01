using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.SiliconStanding;

[RegisterComponent, NetworkedComponent]
public sealed partial class SiliconStandingComponent : Component
{
    [DataField]
    public bool Active = false;
}