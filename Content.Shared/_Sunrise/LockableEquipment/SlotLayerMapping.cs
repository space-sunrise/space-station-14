using Content.Shared.Inventory;

namespace Content.Shared._Sunrise.LockableEquipment;

/// <summary>
/// Fallback slot-based blocking rules used when clothing has no explicit layer metadata.
/// </summary>
public static class SlotLayerMapping
{
    /// <summary>
    /// Fallback rules checked when no explicit <see cref="LayerBlockingComponent"/> is present.
    /// </summary>
    public static readonly SlotBlockRule[] Rules =
    {
        new(SlotFlags.OUTERCLOTHING, 5, "lockable_over", "lockable_normal", "lockable_chest", "lockable_under", "lockable_underpants"),
        new(SlotFlags.INNERCLOTHING, 3, "lockable_under", "lockable_underpants"),
        new(SlotFlags.PANTS, 2, "lockable_under", "lockable_underpants"),
        new(SlotFlags.HEAD, 1, "lockable_head"),
        new(SlotFlags.EYES, 1, "lockable_eyes"),
        new(SlotFlags.MASK, 1, "lockable_face"),
        new(SlotFlags.NECK, 1, "lockable_neck"),
        new(SlotFlags.BACK, 1, "lockable_back"),
        new(SlotFlags.BELT, 1, "lockable_belt"),
        new(SlotFlags.FEET, 1, "lockable_feet"),
        new(SlotFlags.GLOVES, 1, "lockable_hands"),
        new(SlotFlags.EARS, 1, "lockable_ears"),
    };

    /// <summary>
    /// Describes what layers a slot hides and with what fallback priority.
    /// </summary>
    public readonly struct SlotBlockRule
    {
        public readonly SlotFlags Flags;
        public readonly int Priority;
        public readonly string[] Layers;

        public SlotBlockRule(SlotFlags flags, int priority, params string[] layers)
        {
            Flags = flags;
            Priority = priority;
            Layers = layers;
        }
    }
}
