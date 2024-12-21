namespace Content.Shared.Standing;

public sealed class DropHandItemsEvent : EventArgs
{
}

public sealed class DownAttemptEvent : CancellableEntityEventArgs
{
}

public sealed class StandAttemptEvent : CancellableEntityEventArgs
{
}

public sealed class StoodEvent : EntityEventArgs
{
}

public sealed class DownedEvent : EntityEventArgs
{
}
