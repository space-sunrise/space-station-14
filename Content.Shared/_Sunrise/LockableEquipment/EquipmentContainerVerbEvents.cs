namespace Content.Shared._Sunrise.LockableEquipment;

[ByRefEvent]
public record struct EquipmentContainerUseHeldKeyVerbEvent(EntityUid User);

[ByRefEvent]
public record struct EquipmentContainerBreakWithHeldToolVerbEvent(EntityUid User);

[ByRefEvent]
public record struct EquipmentContainerRemoveVerbEvent(EntityUid User);
