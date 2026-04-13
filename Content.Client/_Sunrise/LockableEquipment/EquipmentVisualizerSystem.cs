using Content.Shared._Sunrise.LockableEquipment;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.LockableEquipment;

public sealed class EquipmentVisualizerSystem : VisualizerSystem<EquipmentContainerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, EquipmentContainerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var sprite = args.Sprite;

        if (AppearanceSystem.TryGetData<string>(uid, EquipmentVisuals.IconState, out var iconState, args.Component) &&
            !string.IsNullOrEmpty(iconState))
        {
            SpriteSystem.LayerSetRsiState((uid, sprite), 0, iconState);
        }

        if (!AppearanceSystem.TryGetData<EquipmentVisualData>(uid, EquipmentVisuals.VisualData, out var visualData, args.Component) ||
            visualData == null ||
            string.IsNullOrEmpty(visualData.Layer))
        {
            return;
        }

        var layer = SpriteSystem.LayerMapReserve((uid, sprite), visualData.Layer);

        if (!visualData.Visible ||
            string.IsNullOrEmpty(visualData.RsiPath) ||
            string.IsNullOrEmpty(visualData.State))
        {
            SpriteSystem.LayerSetVisible((uid, sprite), layer, false);
            return;
        }

        SpriteSystem.LayerSetRsi((uid, sprite), layer, new ResPath(visualData.RsiPath));
        SpriteSystem.LayerSetRsiState((uid, sprite), layer, visualData.State);
        SpriteSystem.LayerSetVisible((uid, sprite), layer, true);
    }
}
