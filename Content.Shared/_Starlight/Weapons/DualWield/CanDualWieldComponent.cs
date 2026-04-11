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
    ///     Penalty applied to fire rate when dual-wielding.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DualWieldFireRatePenalty = 0f;

    /// <summary>
    ///     Multiplier applied to recoil when dual-wielding.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DualWieldRecoilPenalty = 0.75f;
}
