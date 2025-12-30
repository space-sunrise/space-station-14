using Content.Shared.NPC.Prototypes;
using Content.Shared.Sunrise.FactionGunBlockerSystem;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.FactionWeaponBlockerSystem;

[RegisterComponent]
public sealed partial class FactionWeaponBlockerComponent : SharedFactionWeaponBlockerComponent
{
    [ViewVariables]
    public bool CanUse;

    [DataField]
    public string AlertText = "weapon-biocode-refused";

    [DataField(required: true)]
    public HashSet<ProtoId<NpcFactionPrototype>> Factions = [];
}
