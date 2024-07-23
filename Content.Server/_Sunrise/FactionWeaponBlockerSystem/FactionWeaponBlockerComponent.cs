using Content.Shared.NPC.Prototypes;
using Content.Shared.Sunrise.FactionGunBlockerSystem;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server._Sunrise.FactionWeaponBlockerSystem;

[RegisterComponent]
public sealed partial class FactionWeaponBlockerComponent : SharedFactionWeaponBlockerComponent
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanUse;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("alertText")]
    public string AlertText = "";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("factions", customTypeSerializer:typeof(PrototypeIdHashSetSerializer<NpcFactionPrototype>))]
    public HashSet<string> Factions = new();
}
