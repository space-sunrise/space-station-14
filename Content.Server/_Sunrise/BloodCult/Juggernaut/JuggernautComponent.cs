using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.BloodCult.Juggernaut;

[RegisterComponent]
public sealed partial class JuggernautComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite),
     DataField("hummerSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string HummerSpawnId = "HammerJuggernaut";
}
