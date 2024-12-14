using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.BecomeDustOnDeathSystem;

[RegisterComponent]
public sealed partial class BecomeDustOnDeathComponent : Component
{
    [DataField("sprite", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnOnDeathPrototype = "Ectoplasm";
}
