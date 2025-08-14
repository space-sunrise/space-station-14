using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.Abilities.Resomi
{
    [RegisterComponent]
    public sealed partial class ResomiSkillComponent : Component
    {
        /// <summary>
        /// ID действия для прыжка
        /// </summary>
        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ActionJumpId = "Jump";

        /// <summary>
        /// Скорость броска при прыжке
        /// </summary>
        [DataField]
        public float ThrowSpeed = 7F;

        /// <summary>
        /// Дальность броска при прыжке
        /// </summary>
        [DataField]
        public float ThrowRange = 5F;

        /// <summary>
        /// Максимальная дальность броска
        /// </summary>
        [DataField]
        public float MaxThrow = 5f;

        /// <summary>
        /// Время через которое компонент ResomiActiveAbility пропадает
        /// </summary>
        [DataField]
        public TimeSpan ExpireTime = TimeSpan.FromSeconds(1);
    }
}
