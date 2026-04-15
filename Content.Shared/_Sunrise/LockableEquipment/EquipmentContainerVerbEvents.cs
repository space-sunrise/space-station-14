using Robust.Shared.GameObjects;

namespace Content.Shared._Sunrise.LockableEquipment;

/// <summary>
/// Executes the lock/unlock action using a suitable held key.
/// </summary>
public sealed class EquipmentContainerUseHeldKeyVerbEvent(EntityUid user) : EntityEventArgs
{
    /// <summary>
    /// User that triggered the verb.
    /// </summary>
    public EntityUid User { get; } = user;
}

/// <summary>
/// Executes the break action using a suitable held tool.
/// </summary>
public sealed class EquipmentContainerBreakWithHeldToolVerbEvent(EntityUid user) : EntityEventArgs
{
    /// <summary>
    /// User that triggered the verb.
    /// </summary>
    public EntityUid User { get; } = user;
}

/// <summary>
/// Executes the remove action for the installed device.
/// </summary>
public sealed class EquipmentContainerRemoveVerbEvent(EntityUid user) : EntityEventArgs
{
    /// <summary>
    /// User that triggered the verb.
    /// </summary>
    public EntityUid User { get; } = user;
}
