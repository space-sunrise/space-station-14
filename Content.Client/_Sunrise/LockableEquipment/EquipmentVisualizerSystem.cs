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

        if (!AppearanceSystem.TryGetData<EquipmentVisualData>(uid, EquipmentVisuals.VisualData, out var visualData, args.Component) ||
            visualData == null ||
            string.IsNullOrEmpty(visualData.Layer))
        {
            // No data — hide all previously reserved equipment layers
            // to avoid stale visuals from a prior device installation
            foreach (var key in sprite.LayerMap.Keys)
            {
                if (!key.StartsWith("lockable_"))
                    continue;

                SpriteSystem.LayerSetVisible((uid, sprite), sprite.LayerMap[key], false);
            }

            return;
        }

        var layerIdx = SpriteSystem.LayerMapReserve((uid, sprite), visualData.Layer);

        if (!visualData.Visible ||
            string.IsNullOrEmpty(visualData.RsiPath) ||
            string.IsNullOrEmpty(visualData.State))
        {
            SpriteSystem.LayerSetVisible((uid, sprite), layerIdx, false);
            return;
        }

        SpriteSystem.LayerSetRsi((uid, sprite), layerIdx, new ResPath(visualData.RsiPath));
        SpriteSystem.LayerSetRsiState((uid, sprite), layerIdx, visualData.State);
        SpriteSystem.LayerSetVisible((uid, sprite), layerIdx, true);
    }
}
