using Content.Shared._Sunrise.LockableEquipment;
using Robust.Client.GameObjects;
using System.Linq;

namespace Content.Client._Sunrise.LockableEquipment;

public sealed class LockableEquipmentVisualizerSystem : VisualizerSystem<LockableEquipmentComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, LockableEquipmentComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<string>(uid, EquipmentVisuals.IconState, out var iconState, args.Component) ||
            string.IsNullOrEmpty(iconState))
        {
            return;
        }

        if (!args.Sprite.AllLayers.Any())
            return;

        SpriteSystem.LayerSetRsiState((uid, args.Sprite), 0, iconState);
    }
}
