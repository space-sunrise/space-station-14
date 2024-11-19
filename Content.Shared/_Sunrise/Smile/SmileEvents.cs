using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Smile;

public sealed partial class SmileLoveActionEvent : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class SmileLoveDoAfterEvent : SimpleDoAfterEvent;
