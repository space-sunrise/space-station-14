using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Client.GameObjects;

using Content.Shared._Sunrise.LockableEquipment;

namespace Content.Client._Sunrise.LockableEquipment
{
    public sealed class EquipmentOverlaySystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<EquipmentOverlayComponent, ComponentHandleState>(OnState);
        }

        private static readonly Dictionary<string, int> LayerOffsets = new()
        {
            { "underwear", -20 },
            { "underpants", -10 },
            { "overpants", +10 },
            { "belt", +20 },
        };

        private const int BaseOrder = 50;

        private void OnState(Entity<EquipmentOverlayComponent> ent, ref ComponentHandleState args)
        {
            UpdateOverlay(ent.Owner, ent.Comp);
        }

        private void UpdateOverlay(EntityUid uid, EquipmentOverlayComponent comp)
        {
            if (!TryComp(uid, out SpriteComponent? sprite))
                return;

            if (!comp.Visible)
            {
                HideLayer(sprite, comp.Layer);
                return;
            }

            var order = ResolveOrder(comp.Layer);

            var layer = EnsureLayer(sprite, comp);

            ApplyLayerState(sprite, layer, comp);
        }

        private void HideLayer(SpriteComponent sprite, string layerKey)
        {
            if (sprite.LayerMapTryGet(layerKey, out var layer))
                sprite.LayerSetVisible(layer, false);
        }

        private int ResolveOrder(string layer)
        {
            if (LayerOffsets.TryGetValue(layer, out var offset))
                return BaseOrder + offset;

            return BaseOrder;
        }

        private int EnsureLayer(SpriteComponent sprite, EquipmentOverlayComponent comp)
        {
            if (!sprite.LayerMapTryGet(comp.Layer, out var layer))
            {
                layer = sprite.AddLayer(comp.SpritePath, comp.State);
                sprite.LayerMapSet(comp.Layer, layer);
            }

            return layer;
        }

        private void ApplyLayerState(SpriteComponent sprite, int layer, EquipmentOverlayComponent comp)
        {
            sprite.LayerSetState(layer, comp.State);
            sprite.LayerSetVisible(layer, true);
        }
    }
}