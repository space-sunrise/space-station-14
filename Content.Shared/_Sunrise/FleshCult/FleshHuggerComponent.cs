﻿using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.FleshCult
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class FleshHuggerComponent : Component
    {
        [DataField("actionJump", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ActionFleshHuggerJumpId = "FleshHuggerJump";

        [DataField("actionGetOff", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ActionFleshHuggerGetOffId = "FleshHuggerGetOff";

        [DataField("paralyzeTime"), ViewVariables(VVAccess.ReadWrite)]
        public float ParalyzeTime = 3f;

        [DataField("chansePounce"), ViewVariables(VVAccess.ReadWrite)]
        public int ChansePounce = 33;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        public bool IsDeath = false;

        public EntityUid EquipedOn;

        [ViewVariables] public float Accumulator = 0;

        [DataField("damageFrequency"), ViewVariables(VVAccess.ReadWrite)]
        public float DamageFrequency = 5;

        [ViewVariables(VVAccess.ReadWrite), DataField("soundJump")]
        public SoundSpecifier? SoundJump = new SoundPathSpecifier("/Audio/_Sunrise/FleshCult/flesh_worm_scream.ogg");
    }
}
