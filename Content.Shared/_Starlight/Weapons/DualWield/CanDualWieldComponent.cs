using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Weapons.DualWield;

/// <summary>
///     Indicates that a weapon can be used in dual-wielding mode.
///     Defines the penalties applied when dual-wielding is active.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CanDualWieldComponent : Component
{
    /// <summary>
    ///     Multiplier applied to the weapon's angle increase per shot when dual-wielding.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DualWieldInaccuracyPenalty = 1f;

    /// <summary>
    ///     Multiplier applied to damage dealt when dual-wielding.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DualWieldDamagePenalty = 0.35f;

    /// <summary>
    ///     Fraction subtracted from the dual-wield fire-rate multiplier as <c>1f - penalty</c>.
    ///     This is applied to each gun individually while dual-wielding, so <c>0.4f</c> makes each gun fire at
    ///     <c>60%</c> of its base rate, <c>0f</c> keeps the base fire rate, and <c>1f</c> would reduce it to zero.
    ///     <seealso cref="SharedDualWieldSystem"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DualWieldFireRatePenalty = 0f;

    /// <summary>
    ///     Multiplier applied to recoil when dual-wielding.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DualWieldRecoilPenalty = 0.75f;
}
