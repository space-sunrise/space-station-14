using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Effects;

/// <summary>
/// Configures spark effects for damageable machines.
/// </summary>
[RegisterComponent]
public sealed partial class MachineSparksComponent : Component
{
    /// <summary>
    /// Effects shown when the machine is hit.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntProtoId> ImpactEffects = [];

    /// <summary>
    /// Effects periodically shown while the machine has low health.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntProtoId> LowHealthEffects = [];

    /// <summary>
    /// Fraction of the destruction threshold at which the machine is considered heavily damaged.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float LowHealthDamageFraction = 0.75f;

    /// <summary>
    /// Minimum delay between periodic spark effects.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan MinLowHealthSparkDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Maximum delay between periodic spark effects.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan MaxLowHealthSparkDelay = TimeSpan.FromSeconds(8);
}
