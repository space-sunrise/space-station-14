using Content.Shared.Dragon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Dragon;

[RegisterComponent]
public sealed partial class DragonRiftComponent : SharedDragonRiftComponent
{
    /// <summary>
    /// Dragon that spawned this rift.
    /// </summary>
    [DataField("dragon")] public EntityUid? Dragon;

    /// <summary>
    /// How long the rift has been active.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("accumulator")]
    public float Accumulator = 0f;

    /// <summary>
    /// The maximum amount we can accumulate before becoming impervious.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxAccumuluator")] public float MaxAccumulator = 300f;

    /// <summary>
    /// Accumulation of the spawn timer.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("spawnAccumulator")]
    public float SpawnAccumulator = 50f; // Sunrise-Edit

    /// <summary>
    /// How long it takes for a new spawn to be added.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("spawnCooldown")]
    public float SpawnCooldown = 50f; // Sunrise-Edit

    [ViewVariables(VVAccess.ReadWrite), DataField("spawn", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnPrototype = "MobCarpDragon";

    // Sunrise-Start
    [DataField]
    public int MaxAliveCarps = 16;

    [ViewVariables(VVAccess.ReadOnly)]
    public int AliveCarps;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsSpawnAccumulating = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("sharkSpawn", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SharkSpawnPrototype = "MobShark";

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float SharkSpawnCooldown = 180f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float SharkLowHealthThreshold = 100f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float SharkSpawnAccumulator;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool SpawnedSharkAtHalfCharge;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool SpawnedSharkAtSeventyFiveCharge;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool SpawnedSharkAtFullCharge;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool SpawnedSharkAtLowHealth;
    // Sunrise-End
}
