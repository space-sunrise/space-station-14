using System.Collections.Generic;
using System.Linq;
using Content.Shared.Roles;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.SponsorInventory;

/// <summary>
/// Shared validation for sponsor inventory profile data received from clients or external sponsor services.
/// </summary>
public static class SunriseInventoryValidation
{
    private const int MaxJobSelectionCount = 128;
    private const int MaxSlotSelectionCount = 64;
    private const int MaxBagSelectionCount = 128;
    private const int MaxPrototypeIdLength = 128;
    private const int MaxSlotIdLength = 64;

    /// <summary>
    /// Validates a sponsor inventory profile against server-side sponsor ownership data for a session.
    /// </summary>
    public static SunriseInventoryProfile EnsureValid(
        SunriseInventoryProfile profile,
        ICommonSession session,
        IPrototypeManager prototype,
        ISharedSponsorsManager? sponsors)
    {
        if (sponsors == null)
            return new SunriseInventoryProfile();

        var config = sponsors.GetSponsorInventoryConfig();
        if (config == null || config.Items is not { Length: > 0 })
            return new SunriseInventoryProfile();

        var purchasedItems = GetPurchasedItems(session, sponsors);
        var sponsorTier = sponsors.GetSponsorTier(session.UserId);

        return EnsureValid(profile, prototype, config, purchasedItems, sponsorTier);
    }

    /// <summary>
    /// Validates a sponsor inventory profile against an already loaded catalog and ownership snapshot.
    /// </summary>
    public static SunriseInventoryProfile EnsureValid(
        SunriseInventoryProfile profile,
        IPrototypeManager prototype,
        SponsorInventoryConfig config,
        IEnumerable<string> purchasedItems,
        int sponsorTier)
    {
        profile ??= new SunriseInventoryProfile();

        if (config == null || config.Items is not { Length: > 0 })
            return new SunriseInventoryProfile();

        var items = new Dictionary<string, SponsorInventoryItemInfo>();
        foreach (var item in config.Items)
        {
            if (item == null || !IsReasonableId(item.Id, MaxPrototypeIdLength))
                continue;

            items[item.Id] = item;
        }

        if (items.Count == 0)
            return new SunriseInventoryProfile();

        var purchased = new HashSet<string>();
        foreach (var purchasedItem in purchasedItems ?? [])
        {
            if (IsReasonableId(purchasedItem, MaxPrototypeIdLength))
                purchased.Add(purchasedItem);
        }

        var valid = new SunriseInventoryProfile
        {
            Global = EnsureValidSelection(
                profile.Global ?? new SunriseInventorySelection(),
                null,
                items,
                purchased,
                sponsorTier,
                prototype),
        };

        var checkedJobs = 0;
        foreach (var (jobId, selection) in profile.Jobs ?? new Dictionary<string, SunriseInventorySelection>())
        {
            if (++checkedJobs > MaxJobSelectionCount)
                break;

            if (!IsReasonableId(jobId, MaxPrototypeIdLength) || !prototype.HasIndex<JobPrototype>(jobId))
                continue;

            var validSelection = EnsureValidSelection(
                selection ?? new SunriseInventorySelection(),
                jobId,
                items,
                purchased,
                sponsorTier,
                prototype);

            if (!validSelection.IsEmpty())
                valid.Jobs[jobId] = validSelection;
        }

        return valid;
    }

    /// <summary>
    /// Returns whether a sponsor inventory item may be used by a session for the selected job.
    /// </summary>
    public static bool CanUseItem(
        string inventoryItemId,
        string? jobId,
        ICommonSession session,
        IPrototypeManager prototype,
        ISharedSponsorsManager? sponsors)
    {
        if (sponsors == null || !IsReasonableId(inventoryItemId, MaxPrototypeIdLength))
            return false;

        var config = sponsors.GetSponsorInventoryConfig();
        SponsorInventoryItemInfo? item = null;

        foreach (var inventoryItem in config.Items ?? [])
        {
            if (inventoryItem == null)
                continue;

            if (inventoryItem.Id != inventoryItemId)
                continue;

            item = inventoryItem;
            break;
        }

        if (item == null)
            return false;

        return CanUseItem(
            item,
            jobId,
            GetPurchasedItems(session, sponsors),
            sponsors.GetSponsorTier(session.UserId),
            prototype);
    }

    /// <summary>
    /// Returns whether a sponsor inventory item may be used with a preloaded catalog and ownership snapshot.
    /// </summary>
    public static bool CanUseItem(
        string inventoryItemId,
        string? jobId,
        IPrototypeManager prototype,
        SponsorInventoryConfig config,
        IEnumerable<string> purchasedItems,
        int sponsorTier)
    {
        if (config == null || !IsReasonableId(inventoryItemId, MaxPrototypeIdLength))
            return false;

        SponsorInventoryItemInfo? item = null;

        foreach (var inventoryItem in config.Items ?? [])
        {
            if (inventoryItem == null)
                continue;

            if (inventoryItem.Id != inventoryItemId)
                continue;

            item = inventoryItem;
            break;
        }

        var purchased = new HashSet<string>();
        foreach (var purchasedItem in purchasedItems ?? [])
        {
            if (IsReasonableId(purchasedItem, MaxPrototypeIdLength))
                purchased.Add(purchasedItem);
        }

        return item != null &&
               CanUseItem(item, jobId, purchased, sponsorTier, prototype);
    }

    /// <summary>
    /// Merges global sponsor inventory choices with job-specific overrides.
    /// </summary>
    public static SunriseInventorySelection GetEffectiveSelection(SunriseInventoryProfile profile, string? jobId)
    {
        profile ??= new SunriseInventoryProfile();

        var selection = new SunriseInventorySelection();
        CopySelectionLayer(profile.Global ?? new SunriseInventorySelection(), selection);

        if (jobId == null ||
            profile.Jobs == null ||
            !profile.Jobs.TryGetValue(jobId, out var jobSelection) ||
            jobSelection == null)
        {
            return selection;
        }

        CopySelectionLayer(jobSelection, selection);
        return selection;
    }

    private static SunriseInventorySelection EnsureValidSelection(
        SunriseInventorySelection selection,
        string? jobId,
        Dictionary<string, SponsorInventoryItemInfo> items,
        HashSet<string> purchasedItems,
        int sponsorTier,
        IPrototypeManager prototype)
    {
        var valid = new SunriseInventorySelection();
        var usedItems = new HashSet<string>();

        var checkedSlotItems = 0;
        foreach (var (slot, itemId) in selection.SlotItems ?? new Dictionary<string, string>())
        {
            if (++checkedSlotItems > MaxSlotSelectionCount)
                break;

            if (!IsReasonableId(slot, MaxSlotIdLength) ||
                !IsReasonableId(itemId, MaxPrototypeIdLength) ||
                usedItems.Contains(itemId) ||
                !items.TryGetValue(itemId, out var item) ||
                !CanUseItem(item, jobId, purchasedItems, sponsorTier, prototype))
            {
                continue;
            }

            valid.SlotItems[slot] = itemId;
            usedItems.Add(itemId);
        }

        var checkedBagItems = 0;
        foreach (var itemId in selection.BagItems ?? [])
        {
            if (++checkedBagItems > MaxBagSelectionCount)
                break;

            if (!IsReasonableId(itemId, MaxPrototypeIdLength) ||
                usedItems.Contains(itemId) ||
                !items.TryGetValue(itemId, out var item) ||
                !CanUseItem(item, jobId, purchasedItems, sponsorTier, prototype))
            {
                continue;
            }

            valid.BagItems.Add(itemId);
            usedItems.Add(itemId);
        }

        return valid;
    }

    private static void CopySelectionLayer(SunriseInventorySelection source, SunriseInventorySelection target)
    {
        var checkedSlotItems = 0;
        foreach (var (slot, itemId) in source.SlotItems ?? new Dictionary<string, string>())
        {
            if (++checkedSlotItems > MaxSlotSelectionCount)
                break;

            if (!IsReasonableId(slot, MaxSlotIdLength) ||
                !IsReasonableId(itemId, MaxPrototypeIdLength) ||
                target.SlotItems.Count >= MaxSlotSelectionCount && !target.SlotItems.ContainsKey(slot))
            {
                continue;
            }

            target.SlotItems[slot] = itemId;
        }

        var checkedBagItems = 0;
        foreach (var itemId in source.BagItems ?? [])
        {
            if (++checkedBagItems > MaxBagSelectionCount ||
                target.BagItems.Count >= MaxBagSelectionCount)
            {
                break;
            }

            if (IsReasonableId(itemId, MaxPrototypeIdLength))
                target.BagItems.Add(itemId);
        }
    }

    private static bool IsReasonableId(string? id, int maxLength)
    {
        return !string.IsNullOrWhiteSpace(id) && id.Length <= maxLength;
    }

    private static bool CanUseItem(
        SponsorInventoryItemInfo item,
        string? jobId,
        HashSet<string> purchasedItems,
        int sponsorTier,
        IPrototypeManager prototype)
    {
        if (!IsReasonableId(item.Id, MaxPrototypeIdLength) ||
            !IsReasonableId(item.EntityPrototype, MaxPrototypeIdLength) ||
            !prototype.HasIndex<EntityPrototype>(item.EntityPrototype))
        {
            return false;
        }

        if (!IsJobAllowed(item, jobId))
            return false;

        if (purchasedItems.Contains(item.Id))
            return true;

        return item.SponsorLevel != null && sponsorTier >= item.SponsorLevel.Value;
    }

    private static bool IsJobAllowed(SponsorInventoryItemInfo item, string? jobId)
    {
        if (item.AvailableJobs is not { Length: > 0 })
            return true;

        if (jobId == null || !IsReasonableId(jobId, MaxPrototypeIdLength))
            return false;

        return item.AvailableJobs.Contains(jobId);
    }

    private static HashSet<string> GetPurchasedItems(ICommonSession session, ISharedSponsorsManager sponsors)
    {
        if (sponsors.TryGetPurchasedInventoryItems(session.UserId, out var serverItems) && serverItems != null)
            return serverItems.ToHashSet();

        return sponsors.GetClientPurchasedInventoryItems()?.ToHashSet() ?? new HashSet<string>();
    }
}
