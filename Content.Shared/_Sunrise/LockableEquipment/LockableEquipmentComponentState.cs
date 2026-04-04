using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.LocableEquipment;

public sealed class LockableEquipmentComponentState : ComponentState
{
    public string Slot;
    public bool Enabled;

    public LockableEquipmentComponentState(string slot, bool enabled)
    {
        Slot = slot;
        Enabled = enabled;
    }
}