using Content.Shared.Sunrise.FactionGunBlockerSystem;

namespace Content.Client._Sunrise.FactionWeaponBlockerSystem;

[RegisterComponent]
public sealed partial class FactionWeaponBlockerComponent : SharedFactionWeaponBlockerComponent
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanUse;

    [ViewVariables(VVAccess.ReadWrite)]
    public string AlertText = "";
}
