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
    }
    
    private void OnAfterNVGUpdateVisualsEvent(EntityUid uid, NVGComponent component, AfterNVGUpdateVisualsEvent args)
    {
        var nvcomp = args.nvcomp;
        
        if (TryComp<SpriteComponent>(component.Owner, out var sprite))
            sprite.LayerSetVisible(NVGVisuals.Light, nvcomp.IsNightVision);
    }
}