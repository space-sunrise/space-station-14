using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.BloodCult.Runes.Comps;

[RegisterComponent]
public sealed partial class CultRuneBloodBoilComponent : Component
{
    [DataField("maxProjectiles"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxProjectiles = 10;

    [DataField("minProjectiles"), ViewVariables(VVAccess.ReadWrite)]
    public int MinProjectiles = 5;

    [DataField("projectilePrototype",
         required: true,
         customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string ProjectilePrototype = default!;

    [DataField("projectileRange"), ViewVariables(VVAccess.ReadWrite)]
    public float ProjectileRange = 50f;

    [DataField("projectileSpeed"), ViewVariables(VVAccess.ReadWrite)]
    public float ProjectileSpeed = 20f;

    [ViewVariables(VVAccess.ReadWrite), DataField("summonMinCount")]
    public uint SummonMinCount = 3;
}
