using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Guardian;

/// <summary>
/// Creates a damaging lightning arc between a guardian and its host.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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
    /// Next time the arc should deal damage.
    /// </summary>
    [NonSerialized]
    public TimeSpan NextUpdate;

    /// <summary>
    /// Host the client overlay should draw the lightning arc to.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? VisualHost;

    /// <summary>
    /// Whether the client overlay should currently draw the arc.
    /// </summary>
    [AutoNetworkedField]
    public bool VisualActive;

    /// <summary>
    /// Sprite used by the client overlay for each lightning segment.
    /// </summary>
    [DataField]
    public SpriteSpecifier ArcSprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Effects/lightning.rsi"), "lightning_6");

    /// <summary>
    /// Extra rotation matching the old beam sprite layer rotation.
    /// </summary>
    [DataField]
    public Angle ArcSpriteRotation = Angle.FromDegrees(180);

    /// <summary>
    /// Color applied to the overlay lightning sprite.
    /// </summary>
    [DataField]
    public Color ArcColor = Color.White;

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
