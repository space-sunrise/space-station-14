using Content.Shared._Sunrise.Eye.NightVision.Components;
using Content.Shared._Sunrise.Eye.NightVision.Systems;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.Eye.NightVision;

public sealed class NVGSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NVGComponent, AfterNVGUpdateVisualsEvent>(OnAfterNVGUpdateVisualsEvent);
        SubscribeLocalEvent<NVGComponent, NVGClientUpdateVisualsEvent>(OnNVGClientUpdateVisualsEvent);
    }
    
    private void OnAfterNVGUpdateVisualsEvent(EntityUid uid, NVGComponent component, AfterNVGUpdateVisualsEvent args)
    {
        var nvcomp = args.nvcomp;
        
        if (TryComp<SpriteComponent>(component.Owner, out var sprite))
        {
            if (sprite.LayerMapTryGet(NVGVisuals.Light, out var layer))
                sprite.LayerSetVisible(layer, !nvcomp.IsNightVision);
        }
    }
    
    private void OnNVGClientUpdateVisualsEvent(EntityUid uid, NVGComponent component, NVGClientUpdateVisualsEvent args)
    {
        var nvcomp = args.nvcomp;
        
        if (TryComp<SpriteComponent>(component.Owner, out var sprite))
        {
            if (sprite.LayerMapTryGet(NVGVisuals.Light, out var layer))
                sprite.LayerSetVisible(layer, args.enable);
        }
    }
}