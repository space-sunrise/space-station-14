using Content.Shared.Anomaly.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Anomaly;

public sealed class AnomalyInjectorMedipenVisualizerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UsedAnomalyInjectorMedipenComponent, ComponentStartup>(OnUsedStartup);
        SubscribeLocalEvent<UsedAnomalyInjectorMedipenComponent, ComponentShutdown>(OnUsedShutdown);
    }

    private void OnUsedStartup(EntityUid uid, UsedAnomalyInjectorMedipenComponent comp, ComponentStartup args)
    {
        if (EntityManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
            sprite.LayerSetState(0, "anomyxine_empty");
    }

    private void OnUsedShutdown(EntityUid uid, UsedAnomalyInjectorMedipenComponent comp, ComponentShutdown args)
    {
        if (EntityManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
            sprite.LayerSetState(0, "anomyxine");
    }
}
