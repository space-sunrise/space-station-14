using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Sunrise.Sandevistan
{
    [RegisterComponent]
    public sealed class SandevistanComponent : Component
    {
        [DataField("duration")]
        public float Duration = 5.0f;

        [DataField("speedMultiplier")]
        public float SpeedMultiplier = 2.0f;

        [DataField("projectileReflectChance")]
        public float ProjectileReflectChance = 0.8f;

        [ViewVariables]
        public bool IsActive = false;
    }
}
