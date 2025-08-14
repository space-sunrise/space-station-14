using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.Abilities.Resomi
{
    [RegisterComponent]
    public sealed partial class ResomiSkillComponent : Component
    {
        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ActionJumpId = "Jump";

        [DataField]
        public float ThrowSpeed = 7F;

        [DataField]
        public float ThrowRange = 5F;

        [DataField]
        public float MaxThrow = 5f;
    }
}
