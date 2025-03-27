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

/// <summary>
/// Raised after an entity falls down.
/// </summary>
public sealed class FellDownEvent : EntityEventArgs
{
    public EntityUid Uid { get; }

    public FellDownEvent(EntityUid uid)
    {
        Uid = uid;
    }
}

/// <summary>
/// Raised on the entity being thrown due to the holder falling down.
/// </summary>
[ByRefEvent]
public record struct FellDownThrowAttemptEvent(EntityUid Thrower, bool Cancelled = false);
