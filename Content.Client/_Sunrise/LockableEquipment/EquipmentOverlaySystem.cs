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
        private void OnState(Entity<EquipmentOverlayComponent> ent, ref ComponentHandleState args)
        {
            UpdateOverlay(ent.Owner, ent.Comp);
        }
        private void UpdateOverlay(EntityUid uid, EquipmentOverlayComponent comp)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;

            if (!sprite.LayerMapTryGet(comp.Layer, out var layer))
            {
                layer = sprite.AddLayer(comp.SpritePath, comp.Layer);
            }

            sprite.LayerSetState(layer, comp.State);
            sprite.LayerSetVisible(layer, comp.Visible);
        }
    }
}
