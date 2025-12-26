using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.EntitySystems;

[Serializable, NetSerializable]
public sealed partial class ToggleSlotDoAfterEvent : SimpleDoAfterEvent;
