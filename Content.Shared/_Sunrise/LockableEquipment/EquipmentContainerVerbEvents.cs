using Robust.Shared.GameObjects;

namespace Content.Shared._Sunrise.LockableEquipment;

/// <summary>
/// Executes the lock/unlock action using a suitable held key.
/// </summary>
public sealed class EquipmentContainerUseHeldKeyVerbEvent : EntityEventArgs
{
    /// <summary>
    /// User that triggered the verb.
    /// </summary>
    public EntityUid User;

    public EquipmentContainerUseHeldKeyVerbEvent(EntityUid user)
    {
        User = user;
    }
}

/// <summary>
/// Executes the break action using a suitable held tool.
/// </summary>
public sealed class EquipmentContainerBreakWithHeldToolVerbEvent : EntityEventArgs
{
    /// <summary>
    /// User that triggered the verb.
    /// </summary>
    public EntityUid User;

    public EquipmentContainerBreakWithHeldToolVerbEvent(EntityUid user)
    {
        User = user;
    }
}

/// <summary>
/// Executes the remove action for the installed device.
/// </summary>
public sealed class EquipmentContainerRemoveVerbEvent : EntityEventArgs
{
    /// <summary>
    /// User that triggered the verb.
    /// </summary>
    public EntityUid User;

    public EquipmentContainerRemoveVerbEvent(EntityUid user)
    {
        User = user;
    }
}
