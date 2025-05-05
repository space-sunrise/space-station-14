using Content.Shared._Sunrise.BloodCult.Pylon;
using Robust.Client.GameObjects;
using SharedPylonComponent = Content.Shared._Sunrise.BloodCult.Pylon.SharedPylonComponent;

namespace Content.Client._Sunrise.BloodCult.Pylon;

public sealed class PylonVisualizerSystem : VisualizerSystem<PylonVisualsComponent>
{
    private const string StateOn = "pylon";
    private const string StateOff = "pylon_off";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedPylonComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, SharedPylonComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite)
            || !AppearanceSystem.TryGetData<bool>(uid, PylonVisualsLayers.Activated, out var activated))
            return;

        sprite.LayerSetState(PylonVisualsLayers.Activated, activated ? StateOn : StateOff);
    }

    protected override void OnAppearanceChange(EntityUid uid,
        PylonVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !AppearanceSystem.TryGetData<bool>(uid, PylonVisuals.Activated, out var activated))
            return;

        args.Sprite.LayerSetState(PylonVisualsLayers.Activated, activated ? component.StateOn : component.StateOff);
    }
}

public enum PylonVisualsLayers : byte
{
    Activated
}
