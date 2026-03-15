using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Dragon
{
    public sealed partial class DragonRiftComponent
    {
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
    }
}
