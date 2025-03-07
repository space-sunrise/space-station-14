﻿using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.FleshCult
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
    public sealed partial class FleshMobComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("soundDeath")]
        public SoundSpecifier? SoundDeath;

        [ViewVariables(VVAccess.ReadWrite),
         DataField("deathMobSpawnId")]
        public EntProtoId DeathMobSpawnId;

        [DataField("deathMobSpawnCount"), ViewVariables(VVAccess.ReadWrite)]
        public int DeathMobSpawnCount;

        [DataField("fleshStatusIcon")]
        public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "FleshFaction";

        [DataField]
        public TimeSpan PopupCooldown = TimeSpan.FromSeconds(3.0);

        [DataField]
        [AutoPausedField]
        public TimeSpan? NextPopupTime;

        [DataField]
        public EntityUid? LastAttackedEntity;

        public bool IsDeath = false;
    }
}
