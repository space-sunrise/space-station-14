using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._Sunrise.LockableEquipment;
using Content.Shared.Inventory;

namespace Content.Server._Sunrise.LockableEquipment;

/// <summary>
/// System that determines whether equipment layers are accessible based on blocking clothing.
/// This system is purely data-driven and relies solely on the LayerBlockingComponent.
/// Any clothing item that should block access to equipment layers must explicitly include a
/// LayerBlockingComponent with the appropriate CoversLayers entries.
/// </summary>
public sealed class LayerAccessSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public bool IsLayerAccessible(EntityUid target, string equipmentLayer, EntityUid? user = null, LockableEquipmentComponent? targetEquipment = null)
    {
        var wearer = ResolveWearer(target);
        if (wearer == EntityUid.Invalid)
            return true;

        if (!TryComp(wearer, out InventoryComponent? inventory))
            return true;

        var targetPriority = GetAccessPriority(target, targetEquipment);

        foreach (var blocker in EnumerateBlockers((wearer, inventory)))
        {
            if (!IsBlockedBy(blocker, equipmentLayer, targetPriority))
                continue;

            if (user.HasValue && CanBypassClothingRestrictions(user.Value, wearer, blocker.Entity))
                continue;

            return false;
        }

        return true;
    }

    private int GetAccessPriority(EntityUid target, LockableEquipmentComponent? targetEquipment)
    {
        if (targetEquipment != null)
            return targetEquipment.AccessPriority;

        return TryComp<LockableEquipmentComponent>(target, out var equipmentComp)
            ? equipmentComp.AccessPriority
            : 0;
    }

    private IEnumerable<LayerAccessBlocker> EnumerateBlockers(Entity<InventoryComponent> wearer)
    {
        foreach (var slot in wearer.Comp.Slots)
        {
            if (!_inventory.TryGetSlotEntity(wearer.Owner, slot.Name, out var entity) || entity is not { } blockerEntity)
                continue;

            if (TryComp(blockerEntity, out LayerBlockingComponent? layerBlocking))
            {
                yield return new LayerAccessBlocker(
                    blockerEntity,
                    layerBlocking.CoversLayers,
                    layerBlocking.AccessPriority);
            }

            if (TryGetSlotFallbackBlocker(slot.SlotFlags, out var fallback))
            {
                fallback.Entity = blockerEntity;
                yield return fallback;
            }
        }
    }

    private bool IsBlockedBy(LayerAccessBlocker blocker, string targetLayer, int targetPriority)
    {
        if (!blocker.CoversLayers.Contains(targetLayer))
            return false;

        return blocker.AccessPriority >= targetPriority;
    }

    private bool TryGetSlotFallbackBlocker(SlotFlags slotFlags, out LayerAccessBlocker blocker)
    {
        foreach (var entry in SlotLayerMapping.SlotBlocksLayers)
        {
            var flags = entry.Key;
            var layers = entry.Value;

            if ((slotFlags & flags) == 0)
                continue;

            blocker = new LayerAccessBlocker(
                EntityUid.Invalid,
                layers,
                SlotLayerMapping.SlotPriorities.GetValueOrDefault(flags, 0));
            return true;
        }

        blocker = new LayerAccessBlocker(EntityUid.Invalid, Array.Empty<string>(), 0);
        return false;
    }

    private bool CanBypassClothingRestrictions(EntityUid user, EntityUid target, EntityUid clothing)
    {
        // Add logic for special access permissions (admin, tools, etc.)
        return false;
    }
    
    private EntityUid ResolveWearer(EntityUid item)
    {
        if (HasComp<InventoryComponent>(item))
            return item;

        var xform = Transform(item);
        var current = xform.ParentUid;
        
        // Walk up the parent chain to find the root owner
        var depth = 0;
        while (current != EntityUid.Invalid && depth < 10)
        {
            depth++;
            if (HasComp<InventoryComponent>(current))
                return current;

            if (!TryComp(current, out TransformComponent? xformComp))
                break;

            current = xformComp.ParentUid;
        }

        return EntityUid.Invalid;
    }

    private sealed class LayerAccessBlocker
    {
        public EntityUid Entity;
        public readonly IEnumerable<string> CoversLayers;
        public readonly int AccessPriority;

        public LayerAccessBlocker(EntityUid entity, IEnumerable<string> coversLayers, int accessPriority)
        {
            Entity = entity;
            CoversLayers = coversLayers;
            AccessPriority = accessPriority;
        }
    }
}
