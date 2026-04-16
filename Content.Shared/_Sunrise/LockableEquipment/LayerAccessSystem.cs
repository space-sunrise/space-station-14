using System.Collections.Generic;
using System.Linq;
using Content.Shared.Inventory;

namespace Content.Shared._Sunrise.LockableEquipment;

/// <summary>
/// Checks whether a lockable layer can be interacted with on the current wearer.
/// </summary>
public sealed class LayerAccessSystem : EntitySystem
{
    private const int WearerResolveDepthLimit = 10;

    [Dependency] private readonly InventorySystem _inventory = default!;

    /// <summary>
    /// Returns false when worn clothing blocks the target lockable layer.
    /// </summary>
    public bool IsLayerAccessible(EntityUid target, string equipmentLayer, LockableEquipmentComponent? targetEquipment = null)
    {
        var wearer = ResolveWearer(target);
        if (wearer == null)
            return true;

        if (!TryComp(wearer.Value, out InventoryComponent? inventory))
            return true;

        return !IsBlocked((wearer.Value, inventory), target, equipmentLayer, GetAccessPriority(target, targetEquipment));
    }

    private int GetAccessPriority(EntityUid target, LockableEquipmentComponent? targetEquipment)
    {
        if (targetEquipment != null)
            return targetEquipment.AccessPriority;

        return TryComp<LockableEquipmentComponent>(target, out var equipmentComp)
            ? equipmentComp.AccessPriority
            : 0;
    }

    private bool IsBlocked(Entity<InventoryComponent> wearer, EntityUid target, string targetLayer, int targetPriority)
    {
        foreach (var blocker in EnumerateBlockers(wearer, target))
        {
            if (!IsBlockedBy(blocker, targetLayer, targetPriority))
                continue;

            return true;
        }

        return false;
    }

    private IEnumerable<LayerAccessBlocker> EnumerateBlockers(Entity<InventoryComponent> wearer, EntityUid target)
    {
        foreach (var slot in wearer.Comp.Slots)
        {
            if (!_inventory.TryGetSlotEntity(wearer.Owner, slot.Name, out var entity) || entity is not { } blockerEntity)
                continue;

            if (blockerEntity == target)
                continue;

            if (TryComp(blockerEntity, out LayerBlockingComponent? layerBlocking))
            {
                yield return new LayerAccessBlocker(
                    layerBlocking.CoversLayers,
                    layerBlocking.AccessPriority);

                continue;
            }

            foreach (var fallback in EnumerateFallbackRules(slot.SlotFlags))
            {
                yield return new LayerAccessBlocker(fallback.Layers, fallback.Priority);
            }
        }
    }

    private bool IsBlockedBy(LayerAccessBlocker blocker, string targetLayer, int targetPriority)
    {
        return blocker.AccessPriority >= targetPriority &&
               blocker.CoversLayers.Contains(targetLayer);
    }

    private IEnumerable<SlotLayerMapping.SlotBlockRule> EnumerateFallbackRules(SlotFlags slotFlags)
    {
        foreach (var candidate in SlotLayerMapping.Rules)
        {
            if ((slotFlags & candidate.Flags) == 0)
                continue;

            yield return candidate;
        }
    }

    private EntityUid? ResolveWearer(EntityUid item)
    {
        if (HasComp<InventoryComponent>(item))
            return item;

        var xform = Transform(item);
        var current = xform.ParentUid;
        var depth = 0;

        while (current.IsValid() && depth < WearerResolveDepthLimit)
        {
            depth++;
            if (HasComp<InventoryComponent>(current))
                return current;

            if (!TryComp(current, out TransformComponent? xformComp))
                break;

            current = xformComp.ParentUid;
        }

        return null;
    }

    private readonly record struct LayerAccessBlocker(
        IReadOnlyCollection<string> CoversLayers,
        int AccessPriority);
}
