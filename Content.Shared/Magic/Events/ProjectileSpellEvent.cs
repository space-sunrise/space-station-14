using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Events;

public sealed partial class ProjectileSpellEvent : WorldTargetActionEvent
{
    /// <summary>
    /// What entity should be spawned.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;

    // Sunrise start
    /// <summary>
    /// Projectile launch speed.
    /// </summary>
    [DataField]
    public float Speed = 25f;
    // Sunrise end
}
