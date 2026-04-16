using Robust.Shared.GameObjects;

namespace Content.Shared._Sunrise.LockableEquipment;

/// <summary>
/// Executes the lock/unlock action using a suitable held key.
/// </summary>
[ByRefEvent]
public readonly record struct EquipmentContainerUseHeldKeyVerbEvent(EntityUid User);

/// <summary>
/// Executes the break action using a suitable held tool.
/// </summary>
[ByRefEvent]
public readonly record struct EquipmentContainerBreakWithHeldToolVerbEvent(EntityUid User);

/// <summary>
/// Executes the remove action for the installed device.
/// </summary>
[ByRefEvent]
public readonly record struct EquipmentContainerRemoveVerbEvent(EntityUid User);
