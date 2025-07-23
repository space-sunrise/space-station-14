using System.Linq;
using Content.Server._Sunrise.Helpers;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Research.Artifact.Effects.RandomTransformation;

public sealed class ArtifactRandomTransformationSystem : BaseXAESystem<ArtifactRandomTransformationComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SunriseHelpersSystem _helpers = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    protected override void OnActivated(Entity<ArtifactRandomTransformationComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var coords = Transform(ent).Coordinates;
        var entities = _lookup.GetEntitiesInRange<ItemComponent>(coords, ent.Comp.Radius)
            .Select(e => e.Owner)
            .ToHashSet();

        SearchPlayersInventoryForItems(ent, coords, out var inventoryItems);

        ReduceAndTransform(ent, inventoryItems);
        ReduceAndTransform(ent, entities);
    }

    private void SearchPlayersInventoryForItems(Entity<ArtifactRandomTransformationComponent> ent, EntityCoordinates coords, out HashSet<EntityUid> items)
    {
        var players = _lookup.GetEntitiesInRange<InventoryComponent>(coords, ent.Comp.Radius);
        items = [];

        foreach (var player in players)
        {
            var inventorySlots = _inventory.GetSlotEnumerator(player.Owner);

            while (inventorySlots.MoveNext(out var slot))
            {
                if (!_inventory.TryGetSlotEntity(player, slot.ID, out var itemUid))
                    continue;

                items.Add(itemUid.Value);
            }
        }
    }

    private void ReduceAndTransform(Entity<ArtifactRandomTransformationComponent> ent, IReadOnlyCollection<EntityUid> entities)
    {
        var items = _helpers.GetPercentageOfHashSet(entities, ent.Comp.TransformationPercentRatio);

        DoTransformation(ent, items);
    }

    private void DoTransformation(Entity<ArtifactRandomTransformationComponent> ent, IEnumerable<EntityUid> items)
    {
        foreach (var item in items)
        {
            if (!_prototype.TryGetRandom<EntityPrototype>(_random, out var prototype))
                continue;

            var proto = (EntityPrototype) prototype;

            if (!CanSpawnEntity(ent, proto))
                continue;

            /*
             * TODO: Обработка ентити в контейнерах
             * Требуется сделать проверку, что если ентити находится в контейнере
             * То после создания нового оно помещается в тот же слот контейнера
             */

            Spawn(prototype.ID, _transform.GetMapCoordinates(item));
            QueueDel(item);
        }
    }

    private IEnumerable<string> GetAllParentIds(string protoId)
    {
        if (!_prototype.TryIndex<EntityPrototype>(protoId, out var proto))
            yield break;

        if (proto.Parents == null)
            yield break;

        foreach (var parentId in proto.Parents)
        {
            yield return parentId;

            foreach (var parentParentsId in GetAllParentIds(parentId))
            {
                yield return parentParentsId;
            }
        }
    }

    private bool CanSpawnEntity(Entity<ArtifactRandomTransformationComponent> ent, EntityPrototype proto)
    {
        if (proto.Abstract)
            return false;

        if (ent.Comp.PrototypeBlacklist != null && ent.Comp.PrototypeBlacklist.Contains(proto.ID))
            return false;

        var isException = ent.Comp.PrototypeBlacklistExceptions != null && ent.Comp.PrototypeBlacklistExceptions.Contains(proto.ID);

        if (!isException && ent.Comp.PrototypeBlacklist != null && GetAllParentIds(proto.ID)
                .Any(parentId => ent.Comp.PrototypeBlacklist.Contains(parentId)))
            return false;

        if (ent.Comp.ComponentBlacklist != null && proto.Components.Keys.Any(id => ent.Comp.ComponentBlacklist.Contains(id)))
            return false;

        if (ent.Comp.CategoryBlacklist != null && proto.Categories.Any(c => ent.Comp.CategoryBlacklist.Contains(c)))
            return false;

        return true;
    }
}
