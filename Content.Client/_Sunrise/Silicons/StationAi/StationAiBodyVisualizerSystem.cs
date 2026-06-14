using Content.Shared._Sunrise.Silicons.StationAi;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.Silicons.StationAi;

public sealed class StationAiBodyVisualizerSystem : VisualizerSystem<StationAiBodyComponent>
{
    /// <summary>
    /// Applies the selected station AI body appearance layer to the borg body sprite.
    /// </summary>
    protected override void OnAppearanceChange(EntityUid uid, StationAiBodyComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<PrototypeLayerData>(uid, StationAiBodyVisuals.BodyAppearance, out var layerData, args.Component))
        {
            if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), StationAiBodyVisualLayers.BodyAppearance, out _, false))
                SpriteSystem.LayerSetVisible((uid, args.Sprite), StationAiBodyVisualLayers.BodyAppearance, false);

            return;
        }

        var layer = SpriteSystem.LayerMapReserve((uid, args.Sprite), StationAiBodyVisualLayers.BodyAppearance);
        SpriteSystem.LayerSetData((uid, args.Sprite), layer, layerData);
        SpriteSystem.LayerSetVisible((uid, args.Sprite), layer, true);
    }

    private enum StationAiBodyVisualLayers : byte
    {
        BodyAppearance,
    }
}
