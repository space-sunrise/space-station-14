using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Biocode;

[RegisterComponent]
public sealed partial class BiocodeComponent : Component
{
    [DataField]
    public string AlertText = "item-biocode-refused";

    [DataField(required: true)]
    public HashSet<ProtoId<NpcFactionPrototype>> Factions = new();
}
