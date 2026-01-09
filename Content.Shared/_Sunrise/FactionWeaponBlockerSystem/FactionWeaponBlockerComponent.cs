using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.FactionWeaponBlockerSystem;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FactionWeaponBlockerComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public bool CanUse;

    [DataField]
    public string AlertText = "weapon-biocode-refused";

    [DataField(required: true)]
    public HashSet<ProtoId<NpcFactionPrototype>> Factions = [];
}
