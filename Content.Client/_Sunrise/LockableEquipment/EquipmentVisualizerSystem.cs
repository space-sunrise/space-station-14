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

        private void OnAppearanceChange(Entity<AppearanceComponent> ent, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            var sprite = args.Sprite;

            if (_appearance.TryGetData(ent, EquipmentVisuals.IconState, out string? iconState) &&
                !string.IsNullOrEmpty(iconState))
            {
                _sprite.LayerSetRsiState((ent, sprite), 0, iconState);
            }

            if (!_appearance.TryGetData(ent, EquipmentVisuals.VisualData, out EquipmentVisualData? visualData))
                return;

            if (visualData == null || string.IsNullOrEmpty(visualData.Layer))
                return;

            var layer = _sprite.LayerMapReserve((ent, sprite), visualData.Layer);

            if (!visualData.Visible)
            {
                _sprite.LayerSetVisible((ent, sprite), layer, false);
                return;
            }

            if (string.IsNullOrEmpty(visualData.RsiPath) || string.IsNullOrEmpty(visualData.State))
            {
                _sprite.LayerSetVisible((ent, sprite), layer, false);
                return;
            }

            _sprite.LayerSetRsi((ent, sprite), layer, new ResPath(visualData.RsiPath));
            _sprite.LayerSetRsiState((ent, sprite), layer, visualData.State);
            _sprite.LayerSetVisible((ent, sprite), layer, true);
        }
    }
}
