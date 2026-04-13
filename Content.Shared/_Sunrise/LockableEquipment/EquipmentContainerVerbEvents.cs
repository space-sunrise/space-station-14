using Robust.Shared.GameObjects;

namespace Content.Shared._Sunrise.LockableEquipment;

public sealed class EquipmentContainerUseHeldKeyVerbEvent : EntityEventArgs
{
    public EntityUid User;

    public EquipmentContainerUseHeldKeyVerbEvent(EntityUid user)
    {
        User = user;
    }
}

public sealed class EquipmentContainerBreakWithHeldToolVerbEvent : EntityEventArgs
{
    public EntityUid User;

    public EquipmentContainerBreakWithHeldToolVerbEvent(EntityUid user)
    {
        User = user;
    }
}

public sealed class EquipmentContainerRemoveVerbEvent : EntityEventArgs
{
    public EntityUid User;

    public EquipmentContainerRemoveVerbEvent(EntityUid user)
    {
        User = user;
    }
}
