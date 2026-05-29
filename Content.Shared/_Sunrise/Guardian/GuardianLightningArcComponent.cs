using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Guardian;

/// <summary>
/// Creates a damaging lightning arc between a guardian and its host.
/// </summary>
[RegisterComponent]
public sealed partial class GuardianLightningArcComponent : Component
{
    /// <summary>
    /// Damage dealt every tick to entities touching the arc.
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = { { "Heat", 3 } },
    };

    /// <summary>
    /// How often the arc damages targets.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.1);

    /// <summary>
    /// Width of the damaging line between guardian and host.
    /// </summary>
    [DataField]
    public float ArcWidth = 0.35f;

    /// <summary>
    /// Mobs near a target touching the arc also take damage within this radius.
    /// </summary>
    [DataField]
    public float SplashRadius = 0.75f;

    /// <summary>
    /// Beam prototype used to render the arc.
    /// </summary>
    [DataField]
    public EntProtoId BeamPrototype = "GuardianLightningArc";

    /// <summary>
    /// Next time the arc should deal damage.
    /// </summary>
    [NonSerialized]
    public TimeSpan NextUpdate;

    /// <summary>
    /// Host currently receiving electric resistance from this guardian.
    /// </summary>
    [NonSerialized]
    public EntityUid? ProtectedHost;

    /// <summary>
    /// Whether the arc system added the host insulation component and should remove it later.
    /// </summary>
    [NonSerialized]
    public bool AddedHostInsulation;
}
