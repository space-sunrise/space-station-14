// Sunrise-Edit

using Content.Shared._Sunrise.Weapons.Enums;
using Content.Shared.Inventory;

namespace Content.Shared._Sunrise.Weapons.Events;

[ByRefEvent]
public record struct HitScanPierceAttemptEvent(PierceLevel Level, bool Pierced) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;
}
