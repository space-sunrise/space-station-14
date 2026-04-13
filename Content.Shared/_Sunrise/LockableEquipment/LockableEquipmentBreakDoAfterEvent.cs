using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.LockableEquipment;

[Serializable, NetSerializable]
public sealed partial class LockableEquipmentBreakDoAfterEvent : SimpleDoAfterEvent;
