using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server._Sunrise.FactionClothingBlockerSystem;

[RegisterComponent]
public sealed partial class FactionClothingBlockerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite),
     DataField("factions", customTypeSerializer:typeof(PrototypeIdHashSetSerializer<NpcFactionPrototype>))]
    public HashSet<string> Factions = new();

    [DataField("beepSound")]
    public SoundSpecifier BeepSound = new SoundPathSpecifier("/Audio/Effects/beep1.ogg");
}
