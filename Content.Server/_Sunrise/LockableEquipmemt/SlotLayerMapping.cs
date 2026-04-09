using Content.Shared.Inventory;

namespace Content.Server._Sunrise.LockableEquipment;

/// <summary>
/// Fallback mapping used when clothing has no explicit <see cref="LayerBlockingComponent"/>.
/// Keep this conservative and prefer explicit prototype data where possible.
/// </summary>
public static class SlotLayerMapping
{
    /// <summary>
    /// Maps slot flags to the equipment layers they block
    /// </summary>
    public static readonly Dictionary<SlotFlags, HashSet<string>> SlotBlocksLayers = new()
    {
        // Outer clothing blocks access to everything underneath
        { SlotFlags.OUTERCLOTHING, new HashSet<string> { "lockable_over", "lockable_chest", "lockable_under", "lockable_underpants" } },
        
        
        // Head gear blocks head-related layers
        { SlotFlags.HEAD, new HashSet<string> { "lockable_head" } },
        
        // Eyes block eye-related layers
        { SlotFlags.EYES, new HashSet<string> { "lockable_eyes" } },
        
        // Mask blocks face-related layers
        { SlotFlags.MASK, new HashSet<string> { "lockable_face" } },
        
        // Neck blocks neck-related layers
        { SlotFlags.NECK, new HashSet<string> { "lockable_neck" } },
        
        // Backpack blocks back-related layers
        { SlotFlags.BACK, new HashSet<string> { "lockable_back" } },
        
        // Belt blocks belt-related layers
        { SlotFlags.BELT, new HashSet<string> { "lockable_belt" } },
        
        // Pants block leg-related layers
        { SlotFlags.PANTS, new HashSet<string> { "lockable_under", "lockable_underpants" } },
        
        // Inner clothing blocks under layers
        { SlotFlags.INNERCLOTHING, new HashSet<string> { "lockable_under", "lockable_underpants" } },
        
        // Footwear blocks foot-related layers
        { SlotFlags.FEET, new HashSet<string> { "lockable_feet" } },
        
        // Hands block hand-related layers
        { SlotFlags.GLOVES, new HashSet<string> { "lockable_hands" } },
        
        // Ears block ear-related layers
        { SlotFlags.EARS, new HashSet<string> { "lockable_ears" } },
    };

    /// <summary>
    /// Defines the priority of slots - higher priority slots block access to lower priority slots
    /// </summary>
    public static readonly Dictionary<SlotFlags, int> SlotPriorities = new()
    {
        { SlotFlags.OUTERCLOTHING, 5 },
        { SlotFlags.INNERCLOTHING, 3 },
        { SlotFlags.PANTS, 2 },
        { SlotFlags.NONE, 1 }, // Default lowest priority
        // Add other slot priorities as needed
    };
}
