using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Weapons.DualWield;

/// <summary>
///     Marks an entity as currently dual-wielding two weapons.
///     Holds runtime state about which guns are in which hand and the next shot side.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DualWieldComponent : Component
{
    /// <summary>
    ///     Is dual-wielding currently active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active = false;

    /// <summary>
    ///     The entity UID of the weapon in the left hand.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? LeftGun;

    /// <summary>
    ///     The entity UID of the weapon in the right hand.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? RightGun;

    /// <summary>
    ///     Legacy flag from the older alternating-shot implementation.
    ///     Kept for network compatibility so existing serialized state remains valid.
    /// </summary>
    [AutoNetworkedField]
    public bool NextIsLeft = true;
}
