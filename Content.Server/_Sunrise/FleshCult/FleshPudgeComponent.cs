using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.FleshCult
{
    [RegisterComponent]
    public sealed partial class FleshPudgeComponent : Component
    {
        [DataField("actionThrowWorm", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ActionThrowWormId = "FleshThrowWorm";

        [DataField("actionAcidSpit", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ActionAcidSpitId = "FleshAcidSpit";

        [DataField("actionAbsorbBloodPool", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ActionAbsorbBloodPoolId = "AbsorbBloodPool";

        [ViewVariables(VVAccess.ReadWrite), DataField("soundThrowWorm")]
        public SoundSpecifier? SoundThrowWorm = new SoundPathSpecifier("/Audio/_Sunrise/FleshCult/throw_worm.ogg");

        [ViewVariables(VVAccess.ReadWrite),
         DataField("faceHuggerMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string FaceHuggerMobSpawnId = "MobFleshHugger";

        [ViewVariables(VVAccess.ReadWrite),
         DataField("bulletAcidSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string BulletAcidSpawnId = "BulletSplashAcid";

        [DataField("healBloodAbsorbReagents")] public Solution HealBloodAbsorbReagents = new()
        {
            Contents =
            {
                new ReagentQuantity(new ReagentId("Carol", null), 1),
            }
        };

        [ViewVariables(VVAccess.ReadWrite), DataField("bloodWhitelist")]
        public List<string> BloodWhitelist = new()
        {
            "Blood",
            "CopperBlood",
            "InsectBlood",
            "AmmoniaBlood",
            "ZombieBlood"
        };

        [DataField("bloodAbsorbSound")]
        public SoundSpecifier BloodAbsorbSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");
    }
}
