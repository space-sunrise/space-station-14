using Content.Shared._Sunrise.BloodCult;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.BloodCult.Structures;

public sealed class CultCraftStructureVisualizerSystem : VisualizerSystem<CultCraftStructureVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid,
        CultCraftStructureVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (args.Sprite == null
            || !AppearanceSystem.TryGetData<bool>(uid, CultCraftStructureVisuals.Activated, out var activated))
            return;

        args.Sprite.LayerSetState(CultCraftStructureVisualsLayers.Activated,
            activated ? component.StateOn : component.StateOff);
    }
}

public enum CultCraftStructureVisualsLayers : byte
{
    Activated
}
