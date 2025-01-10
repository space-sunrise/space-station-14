using Content.Shared._Sunrise.Eye.NightVision.Components;
using Content.Shared._Sunrise.Eye.NightVision.Systems;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.Eye.NightVision;

public sealed class NightVisionDeviceVisualsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionDeviceComponent, AfterNvdUpdateVisualsEvent>(OnAfterNVGUpdateVisualsEvent);
    }

    private void OnAfterNVGUpdateVisualsEvent(EntityUid uid, NightVisionDeviceComponent component, AfterNvdUpdateVisualsEvent args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            if (sprite.LayerMapTryGet(NVDVisuals.Light, out var layer))
                sprite.LayerSetVisible(layer, component.Activated);
        }
    }
}
