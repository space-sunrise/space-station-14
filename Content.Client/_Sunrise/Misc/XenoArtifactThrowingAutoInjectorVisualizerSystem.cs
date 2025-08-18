using Content.Shared._Sunrise.Misc;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client._Sunrise.Misc;

public sealed class XenoArtifactThrowingAutoInjectorVisualizerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UsedXenoArtifactThrowingAutoInjectorComponent, ComponentStartup>(OnUsedStartup);
        SubscribeLocalEvent<UsedXenoArtifactThrowingAutoInjectorComponent, ComponentShutdown>(OnUsedShutdown);
    }

    private void OnUsedStartup(EntityUid uid, UsedXenoArtifactThrowingAutoInjectorComponent comp, ComponentStartup args)
    {
        SetSpriteState(uid, comp.SpriteStateEmpty, comp.SpriteLayerName);
    }

    private void OnUsedShutdown(EntityUid uid, UsedXenoArtifactThrowingAutoInjectorComponent comp, ComponentShutdown args)
    {
        SetSpriteState(uid, comp.SpriteStateFull, comp.SpriteLayerName);
    }

    private void SetSpriteState(EntityUid uid, string state, string layerName)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite) && sprite.LayerMapTryGet(layerName, out var layer))
        {
            sprite.LayerSetState(layer, state);
        }
    }
}
