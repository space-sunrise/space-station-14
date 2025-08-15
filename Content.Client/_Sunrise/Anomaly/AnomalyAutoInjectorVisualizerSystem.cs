// TODO: создана временная визуализация, т.к. визуализатор medipen не подходит из-за метаболизма. В будущем стоит сделать с системой визуализации medipen.
using Content.Shared._Sunrise.Anomaly.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client._Sunrise.Anomaly;

public sealed class AnomalyAutoInjectorVisualizerSystem : EntitySystem
{
    private const string SpriteLayerName = "base";
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UsedAnomalyAutoInjectorComponent, ComponentStartup>(OnUsedStartup);
        SubscribeLocalEvent<UsedAnomalyAutoInjectorComponent, ComponentShutdown>(OnUsedShutdown);
    }

    private void OnUsedStartup(EntityUid uid, UsedAnomalyAutoInjectorComponent comp, ComponentStartup args)
    {
        if (_entityManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
            sprite.LayerSetState(SpriteLayerName, comp.SpriteStateEmpty);
    }

    private void OnUsedShutdown(EntityUid uid, UsedAnomalyAutoInjectorComponent comp, ComponentShutdown args)
    {
        if (_entityManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
            sprite.LayerSetState(SpriteLayerName, comp.SpriteStateFull);
    }
}
