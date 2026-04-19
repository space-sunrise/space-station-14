using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Weapons.DualWield;

/// <summary>
///     Marks an entity as currently dual-wielding two weapons.
///     Holds runtime state about which guns are in which hand and the firing queue.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DualWieldComponent : Component
{
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
    ///     Ordered queue of gun entity UIDs. The gun at index 0 fires next.
    ///     After each shot attempt the front gun is moved to the back.
    /// </summary>
    [AutoNetworkedField]
    public List<EntityUid> GunQueue = new();
}
