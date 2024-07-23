using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Sunrise.FactionGunBlockerSystem;

[NetworkedComponent]
public abstract partial class SharedFactionWeaponBlockerComponent : Component
{

}

[Serializable, NetSerializable]
public sealed class FactionWeaponBlockerComponentState : ComponentState
{
    public bool CanUse;
    public string AlertText = "";
}
