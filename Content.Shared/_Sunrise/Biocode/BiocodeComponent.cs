using Content.Shared.NPC.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared._Sunrise.Biocode;

[RegisterComponent]
public sealed partial class BiocodeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("alertText")]
    public string AlertText = "";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("factions", customTypeSerializer:typeof(PrototypeIdHashSetSerializer<NpcFactionPrototype>))]
    public HashSet<string> Factions = new();
}
