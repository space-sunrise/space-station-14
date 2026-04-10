using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._Sunrise.LockableEquipment;
using Content.Shared.Inventory;

namespace Content.Server._Sunrise.LockableEquipment;

/// <summary>
/// Checks whether a lockable layer can be interacted with on the current wearer.
/// </summary>
public sealed class LayerAccessSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    /// <summary>
    /// Returns false when worn clothing blocks the target lockable layer.
    /// </summary>
    public bool IsLayerAccessible(EntityUid target, string equipmentLayer, LockableEquipmentComponent? targetEquipment = null)
    {
        var wearer = ResolveWearer(target);
        if (wearer == EntityUid.Invalid)
            return true;

        if (!TryComp(wearer, out InventoryComponent? inventory))
            return true;

        return !IsBlocked((wearer, inventory), equipmentLayer, GetAccessPriority(target, targetEquipment));
    }

    /// <summary>
    /// Returns the effective priority of the target device.
    /// </summary>
    private int GetAccessPriority(EntityUid target, LockableEquipmentComponent? targetEquipment)
    {
        if (targetEquipment != null)
            return targetEquipment.AccessPriority;

        return TryComp<LockableEquipmentComponent>(target, out var equipmentComp)
            ? equipmentComp.AccessPriority
            : 0;
    }

    /// <summary>
    /// Returns true when any worn item blocks the requested layer.
    /// </summary>
    private bool IsBlocked(Entity<InventoryComponent> wearer, string targetLayer, int targetPriority)
    {
        foreach (var blocker in EnumerateBlockers(wearer))
        {
            if (!IsBlockedBy(blocker, targetLayer, targetPriority))
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Enumerates explicit and fallback blockers from currently equipped clothing.
    /// </summary>
    private IEnumerable<LayerAccessBlocker> EnumerateBlockers(Entity<InventoryComponent> wearer)
    {
        foreach (var slot in wearer.Comp.Slots)
        {
            if (!_inventory.TryGetSlotEntity(wearer.Owner, slot.Name, out var entity) || entity is not { } blockerEntity)
                continue;

            if (TryComp(blockerEntity, out LayerBlockingComponent? layerBlocking))
            {
                yield return new LayerAccessBlocker(
                    layerBlocking.CoversLayers,
                    layerBlocking.AccessPriority);
            }

            if (TryGetFallbackRule(slot.SlotFlags, out var fallback))
            {
                yield return new LayerAccessBlocker(fallback.Layers, fallback.Priority);
            }
        }
    }

    /// <summary>
    /// Applies the shared blocking rule: matching layer and sufficient priority.
    /// </summary>
    private bool IsBlockedBy(LayerAccessBlocker blocker, string targetLayer, int targetPriority)
    {
        return blocker.AccessPriority >= targetPriority &&
               blocker.CoversLayers.Contains(targetLayer);
    }

    /// <summary>
    /// Returns the first fallback slot rule matching the current slot flags.
    /// </summary>
    private bool TryGetFallbackRule(SlotFlags slotFlags, out SlotLayerMapping.SlotBlockRule rule)
    {
        foreach (var candidate in SlotLayerMapping.Rules)
        {
            if ((slotFlags & candidate.Flags) == 0)
                continue;

            rule = candidate;
            return true;
        }

        rule = default;
        return false;
    }

    /// <summary>
    /// Resolves the inventory owner that ultimately wears or contains the device.
    /// </summary>
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
        public readonly IReadOnlyCollection<string> CoversLayers;
        public readonly int AccessPriority;

        public LayerAccessBlocker(IReadOnlyCollection<string> coversLayers, int accessPriority)
        {
            CoversLayers = coversLayers;
            AccessPriority = accessPriority;
        }
    }
}
