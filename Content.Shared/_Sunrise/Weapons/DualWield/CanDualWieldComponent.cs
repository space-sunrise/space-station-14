using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Weapons.DualWield;

/// <summary>
///     Indicates that a weapon can be used in dual-wielding mode.
///     Defines the penalties applied when dual-wielding is active.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CanDualWieldComponent : Component
{
    /// <summary>
    ///     The alert shown while this weapon is being dual-wielded.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> DualWieldAlert = "DualWieldActive";

    /// <summary>
    ///     Fractional penalty added to the weapon's angle increase per shot when dual-wielding.
    ///     For example, 0.25 means angle is increased by 25%: <c>AngleIncrease *= (1 + penalty)</c>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DualWieldInaccuracyPenalty = 0.25f;

    /// <summary>
    ///     Fractional penalty subtracted from fire rate when dual-wielding.
    ///     For example, 0.25 means fire rate is reduced by 25%: <c>FireRate *= (1 - penalty)</c>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DualWieldFireRatePenalty = 0.25f;

    /// <summary>
    ///     Fractional penalty added to camera recoil when dual-wielding.
    ///     For example, 0.25 means recoil is increased by 25%: <c>CameraRecoilScalar *= (1 + penalty)</c>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DualWieldRecoilPenalty = 0.25f;
}
