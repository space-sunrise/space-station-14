using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Standing;

[Serializable, NetSerializable]
public sealed class DropHandItemsEvent : EventArgs;

[Serializable, NetSerializable]
public sealed class DownAttemptEvent : CancellableEntityEventArgs;

[Serializable, NetSerializable]
public sealed class StandAttemptEvent : CancellableEntityEventArgs;

[Serializable, NetSerializable]
public sealed class StoodEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class DownedEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class ChangeLayingDownEvent : CancellableEntityEventArgs;

[Serializable, NetSerializable]
public sealed partial class StandUpDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class DownDoAfterEvent : SimpleDoAfterEvent;
