using Content.Shared._Sunrise.BloodCult.Items;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.BloodCult.Items.VoidTorch;

public sealed class VoidTorchVisualizerSystem : VisualizerSystem<VoidTorchVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid,
        VoidTorchVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (args.Sprite == null
            || !AppearanceSystem.TryGetData<bool>(uid, VoidTorchVisuals.Activated, out var activated))
            return;

        args.Sprite.LayerSetState(VoidTorchVisualsLayers.Activated, activated ? component.StateOn : component.StateOff);
    }
}

public enum VoidTorchVisualsLayers : byte
{
    Activated
}
