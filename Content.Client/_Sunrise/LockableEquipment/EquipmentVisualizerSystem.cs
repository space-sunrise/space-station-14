using Content.Shared.Humanoid;
using Content.Shared._Sunrise.LockableEquipment;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Log;


namespace Content.Client._Sunrise.LockableEquipment
{
    public sealed class EquipmentVisualizerSystem : EntitySystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly SpriteSystem _sprite = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<AppearanceComponent, AppearanceChangeEvent>(OnAppearanceChange);
        }

        private void OnAppearanceChange(EntityUid uid, AppearanceComponent appearance, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            var sprite = args.Sprite;

            if (!_appearance.TryGetData(uid, EquipmentVisuals.Visible, out bool visible))
                return;

            if (!_appearance.TryGetData(uid, EquipmentVisuals.Layer, out string layerKey))
                return;

            if (string.IsNullOrEmpty(layerKey))
                return;

            var layer = _sprite.LayerMapReserve((uid, sprite), layerKey);

            if (!visible)
            {
                _sprite.LayerSetVisible(uid, layer, false);
                return;
            }

            if (!_appearance.TryGetData(uid, EquipmentVisuals.Sprite, out string rsiPath))
                return;

            if (string.IsNullOrEmpty(rsiPath))
            {
                _sprite.LayerSetVisible(uid, layer, false);
                return;
            }

            _sprite.LayerSetRsi((uid, sprite), layer, new ResPath(rsiPath));
            _sprite.LayerSetRsiState(uid, layer, "equipped");
            _sprite.LayerSetVisible(uid, layer, true);
            Log.Info($"[layer] uid = {uid} visible={visible}");

        }
    }
}
