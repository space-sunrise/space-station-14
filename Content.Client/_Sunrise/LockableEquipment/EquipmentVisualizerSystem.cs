using Content.Shared._Sunrise.LockableEquipment;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

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

            if (!_appearance.TryGetData(uid, EquipmentVisuals.VisualData, out EquipmentVisualData? visualData))
                return;

            if (visualData == null || string.IsNullOrEmpty(visualData.Layer))
                return;

            var layer = _sprite.LayerMapReserve((uid, sprite), visualData.Layer);

            if (!visualData.Visible)
            {
                _sprite.LayerSetVisible((uid, sprite), layer, false);
                return;
            }

            if (string.IsNullOrEmpty(visualData.RsiPath) || string.IsNullOrEmpty(visualData.State))
            {
                _sprite.LayerSetVisible((uid, sprite), layer, false);
                return;
            }

            _sprite.LayerSetRsi((uid, sprite), layer, new ResPath(visualData.RsiPath));
            _sprite.LayerSetRsiState((uid, sprite), layer, visualData.State);
            _sprite.LayerSetVisible((uid, sprite), layer, true);
        }
    }
}
