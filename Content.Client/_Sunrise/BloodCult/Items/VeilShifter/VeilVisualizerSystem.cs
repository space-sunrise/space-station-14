using Content.Shared._Sunrise.BloodCult.Items;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.BloodCult.Items.VeilShifter;

public sealed class VeilVisualizerSystem : VisualizerSystem<VeilVisualsComponent>
{
    private const string StateOn = "icon-on";
    private const string StateOff = "icon";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoidTeleportComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, VoidTeleportComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite)
            || !AppearanceSystem.TryGetData<bool>(uid, VeilVisuals.Activated, out var activated))
            return;

        sprite.LayerSetState(VeilVisualsLayers.Activated, activated ? StateOn : StateOff);
    }

    protected override void OnAppearanceChange(EntityUid uid,
        VeilVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !AppearanceSystem.TryGetData<bool>(uid, VeilVisuals.Activated, out var activated))
            return;

        args.Sprite.LayerSetState(VeilVisualsLayers.Activated, activated ? component.StateOn : component.StateOff);
    }
}

public enum VeilVisualsLayers : byte
{
    Activated
}
