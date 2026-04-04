using Robust.Shared.GameObjects;

namespace Content.Shared._Sunrise.LockableEquipment;

public sealed class UseKeyOnLockEvent : EntityEventArgs
{
    public readonly EntityUid User;
    public readonly EntityUid Used;
    public bool Handled;

    public UseKeyOnLockEvent(EntityUid user, EntityUid used)
    {
        User = user;
        Used = used;
    }
}