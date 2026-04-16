using Content.Shared._Sunrise.LockableEquipment;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.LockableEquipment;

public sealed class LockableEquipmentVisualizerSystem : VisualizerSystem<LockableEquipmentComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, LockableEquipmentComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.Sprite.LayerMapTryGet("base", out var layer))
            return;

        if (!AppearanceSystem.TryGetData<string>(uid, EquipmentVisuals.IconState, out var iconState, args.Component) ||
            string.IsNullOrEmpty(iconState))
        {
            return;
        }

        SpriteSystem.LayerSetRsiState((uid, args.Sprite), layer, iconState);
    }
}
