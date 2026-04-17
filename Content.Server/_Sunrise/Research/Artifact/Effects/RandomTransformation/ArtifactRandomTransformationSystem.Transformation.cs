using Content.Shared._Sunrise.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Research.Artifact.Effects.RandomTransformation;

public sealed partial class ArtifactRandomTransformationSystem
{
    /*
     * Nearby item collection and transformation execution part of the system.
     */

    private void SearchPlayersInventoryForItems(EntityCoordinates coords, float radius)
    {
        _inventories.Clear();
        _lookup.GetEntitiesInRange(coords, radius, _inventories);

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

    private void CopyNearbyItems()
    {
        _worldItems.Clear();
        foreach (var item in _items)
        {
            _worldItems.Add(item.Owner);
        }
    }

    private void TryTransformItems(
        List<EntityUid> entities,
        float transformationRatio,
        IReadOnlyList<EntityPrototype> candidates)
    {
        var countToTransform = GetTransformCount(entities.Count, transformationRatio);
        if (countToTransform <= 0)
            return;

        if (entities.Count > 1)
            entities.ShuffleRobust(_random);

        DoTransformation(entities, countToTransform, candidates);
    }

    private void DoTransformation(List<EntityUid> items, int countToTransform, IReadOnlyList<EntityPrototype> candidates)
    {
        for (var i = 0; i < countToTransform; i++)
        {
            var item = items[i];
            if (Deleted(item))
                continue;

            var prototype = _random.Pick(candidates);

            // Container-contained items are still replaced at map coordinates until slot preservation is implemented.
            Spawn(prototype.ID, _transform.GetMapCoordinates(item));
            QueueDel(item);
        }
    }

    private static int GetTransformCount(int sourceCount, float transformationRatio)
    {
        return Math.Max(1, (int) (sourceCount * transformationRatio));
    }
}
