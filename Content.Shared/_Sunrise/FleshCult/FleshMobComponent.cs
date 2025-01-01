using Content.Shared.Actions;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.FleshCult
{
    [RegisterComponent]
    public sealed partial class FleshMobComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("soundDeath")]
        public SoundSpecifier? SoundDeath = new SoundPathSpecifier("/Audio/_Sunrise/FleshCult/flesh_pudge_dead.ogg");

        [ViewVariables(VVAccess.ReadWrite),
         DataField("deathMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string DeathMobSpawnId = "MobFleshWorm";

        [DataField("deathMobSpawnCount"), ViewVariables(VVAccess.ReadWrite)]
        public int DeathMobSpawnCount;

        [DataField("fleshStatusIcon")]
        public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "FleshFaction";

        public bool IsDeath = false;
    }
}

public sealed partial class FleshPudgeThrowFaceHuggerActionEvent : WorldTargetActionEvent
{

}

public sealed partial class FleshPudgeAcidSpitActionEvent : WorldTargetActionEvent
{

}

public sealed partial class FleshPudgeAbsorbBloodPoolActionEvent : InstantActionEvent
{

}
