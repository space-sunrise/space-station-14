using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server._Sunrise.FleshCult
{
    [RegisterComponent]
    public sealed partial class FleshAbilitiesComponent : Component
    {
        [DataField("startingActions", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public List<string> StartingActions = new();

        [DataField]
        public List<EntityUid> Actions = new();

        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ActionFleshCultistDevourId = "FleshCultistDevour";

        [ViewVariables(VVAccess.ReadWrite), DataField("bloodWhitelist")]
        public List<string> BloodWhitelist = new()
        {
            "Blood",
            "CopperBlood",
            "InsectBlood",
            "AmmoniaBlood",
            "ZombieBlood"
        };

        [DataField("devourTime")] public float DevourTime = 10f;

        [DataField("devourSound")]
        public SoundSpecifier DevourSound = new SoundPathSpecifier("/Audio/_Sunrise/FleshCult/devour_flesh_cultist.ogg");

        [DataField("adrenalinReagents")] public Solution AdrenalinReagents = new()
        {
            Contents = { new ReagentQuantity(new ReagentId("Ephedrine", null), 10) }
        };

        [DataField("healDevourReagents")] public Solution HealDevourReagents = new()
        {
            Contents =
            {
                new ReagentQuantity(new ReagentId("Carol", null), 20),
            }
        };

        [DataField("healBloodAbsorbReagents")] public Solution HealBloodAbsorbReagents = new()
        {
            Contents =
            {
                new ReagentQuantity(new ReagentId("Carol", null), 1),
            }
        };

        [DataField]
        public SoundSpecifier BloodAbsorbSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string BulletAcidSpawnId = "BulletSplashAcid";

        [DataField]
        public SoundSpecifier SoundBulletAcid = new SoundPathSpecifier("/Audio/_Sunrise/FleshCult/flesh_cultist_mutation.ogg");

        [DataField]
        public SoundSpecifier SoundMutation = new SoundPathSpecifier("/Audio/_Sunrise/FleshCult/flesh_cultist_mutation.ogg");

        [DataField("fleshHeartId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)),
         ViewVariables(VVAccess.ReadWrite)]
        public string FleshHeartId = "FleshHeart";

        [ViewVariables(VVAccess.ReadWrite), DataField("soundThrowWorm")]
        public SoundSpecifier? SoundThrowHugger = new SoundPathSpecifier("/Audio/_Sunrise/FleshCult/throw_worm.ogg");

        [ViewVariables(VVAccess.ReadWrite),
         DataField("huggerMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string HuggerMobSpawnId = "MobFleshHugger";
    }
}
