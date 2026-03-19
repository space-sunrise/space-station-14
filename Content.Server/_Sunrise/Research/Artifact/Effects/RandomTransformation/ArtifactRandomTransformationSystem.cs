using System;
using System.Linq;
using Content.Shared._Sunrise.Helpers;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Research.Artifact.Effects.RandomTransformation;

public sealed class ArtifactRandomTransformationSystem : BaseXAESystem<ArtifactRandomTransformationComponent>
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly HashSet<Entity<ItemComponent>> _items = [];
    private readonly HashSet<Entity<InventoryComponent>> _inventories = [];
    private readonly List<EntityUid> _inventoryItems = [];
    private bool _enabled = SunriseCCVars.ArtifactRandomTransformationEnabled.DefaultValue;

    public override void Initialize()
    {
        base.Initialize();

        _configuration.OnValueChanged(
            SunriseCCVars.ArtifactRandomTransformationEnabled,
            enabled => _enabled = enabled,
            true);
    }

    protected override void OnActivated(Entity<ArtifactRandomTransformationComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_enabled)
            return;

        var candidates = GetTransformCandidates(ent);
        if (candidates.Count == 0)
            return;

        var coords = Transform(ent).Coordinates;

        _items.Clear();
        _lookup.GetEntitiesInRange(coords, ent.Comp.Radius, _items);

        SearchPlayersInventoryForItems(ent, coords);

        ReduceAndTransform(ent, _inventoryItems, candidates);
        ReduceAndTransform(ent, _items.Select(e => e.Owner).ToList(), candidates);
    }

    private void SearchPlayersInventoryForItems(Entity<ArtifactRandomTransformationComponent> ent, EntityCoordinates coords)
    {
        _inventories.Clear();
        _lookup.GetEntitiesInRange(coords, ent.Comp.Radius, _inventories);

        _inventoryItems.Clear();
        foreach (var player in _inventories)
        {
            var inventorySlots = _inventory.GetSlotEnumerator(player.AsNullable());

            while (inventorySlots.MoveNext(out var slot))
            {
                if (!_inventory.TryGetSlotEntity(player, slot.ID, out var itemUid))
                    continue;

                _inventoryItems.Add(itemUid.Value);
            }
        }
    }

    private void ReduceAndTransform(
        Entity<ArtifactRandomTransformationComponent> ent,
        List<EntityUid> entities,
        IReadOnlyList<EntityPrototype> candidates)
    {
        var filtered = entities
            .ShuffleRobust(_random)
            .TakePercentage(ent.Comp.TransformationPercentRatio)
            .ToList();

        DoTransformation(filtered, candidates);
    }

    private void DoTransformation(List<EntityUid> items, IReadOnlyList<EntityPrototype> candidates)
    {
        foreach (var item in items)
        {
            if (Deleted(item))
                continue;

            var prototype = _random.Pick(candidates);

            /*
             * TODO: Обработка ентити в контейнерах
             * Требуется сделать проверку, что если ентити находится в контейнере
             * То после создания нового оно помещается в тот же слот контейнера
             */

            Spawn(prototype.ID, _transform.GetMapCoordinates(item));
            QueueDel(item);
        }
    }

    private List<EntityPrototype> GetTransformCandidates(Entity<ArtifactRandomTransformationComponent> ent)
    {
        return _prototype.EnumeratePrototypes<EntityPrototype>()
            .Where(proto => CanTransformInto(ent, proto))
            .ToList();
    }

    public bool CanTransformInto(Entity<ArtifactRandomTransformationComponent> ent, EntityPrototype proto)
    {
        return CanTransformInto(ent.Comp, proto);
    }

    private bool CanTransformInto(ArtifactRandomTransformationComponent component, EntityPrototype proto)
    {
        if (proto.Abstract)
            return false;

        if (!proto.MapSavable)
            return false;

        if (component.RequiredComponents != null &&
            component.RequiredComponents.Any(required => !proto.Components.ContainsKey(required)))
            return false;

        if (component.PrototypeBlacklist != null && component.PrototypeBlacklist.Contains(proto.ID))
            return false;

        var isException = component.PrototypeBlacklistExceptions != null &&
                          component.PrototypeBlacklistExceptions.Contains(proto.ID);

        if (!isException &&
            component.PrototypeBlacklist != null &&
            _prototype.EnumerateAllParents<EntityPrototype>(proto.ID)
                .Any(parent => component.PrototypeBlacklist.Contains(parent.id)))
            return false;

        if (component.ComponentBlacklist != null &&
            proto.Components.Keys.Any(id => component.ComponentBlacklist.Contains(id)))
            return false;

        if (component.CategoryBlacklist != null &&
            proto.Categories.Any(category => component.CategoryBlacklist.Contains(category)))
            return false;

        if (ContainsBlacklistedSubstring(proto.ID, component.PrototypeIdBlacklistSubstrings))
            return false;

        if (ContainsBlacklistedSubstring(proto.SetSuffix, component.PrototypeSuffixBlacklistSubstrings))
            return false;

        return true;
    }

    private static bool ContainsBlacklistedSubstring(string? value, IReadOnlyCollection<string>? blacklist)
    {
        if (string.IsNullOrWhiteSpace(value) || blacklist == null)
            return false;

        foreach (var substring in blacklist)
        {
            if (value.Contains(substring, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
