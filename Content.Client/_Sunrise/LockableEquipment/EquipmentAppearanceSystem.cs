using Content.Shared._Sunrise.LockableEquipment;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Content.Shared.Mobs;

namespace Content.Client._Sunrise.LockableEquipment
{
    public sealed class EquipmentVisualizerSystem : EntitySystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly SpriteSystem _sprite = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<EquipmentContainerComponent, AppearanceChangeEvent>(OnAppearance);
        }

        private void OnAppearance(EntityUid uid, EquipmentContainerComponent comp, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (!_appearance.TryGetData<bool>(uid, EquipmentVisuals.Visible, out var visible))
                return;

            if (!_appearance.TryGetData<string>(uid, EquipmentVisuals.Layer, out var layerKey))
                return;

            var sprite = args.Sprite;

            if (!_sprite.LayerMapTryGet((uid, sprite), layerKey, out var layer))
                return;

            if (!visible)
            {
                _sprite.LayerSetVisible(uid, layer, false);
                return;
            }

            _sprite.LayerSetRsiState(uid, layer, "equipped");
            _sprite.LayerSetVisible(uid, layer, true);
        }
    }
}
