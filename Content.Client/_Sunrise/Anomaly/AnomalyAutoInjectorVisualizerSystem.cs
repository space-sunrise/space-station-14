// ГОВНОКОД: сделал кастомную визуалку, ибо не получилось подключить визуализатор от medipen к автоинъектору, чтоб было без метаболизма, и после применения шла логика AnomalyAutoInjectorSystem
using Content.Shared.Anomaly.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Anomaly;

public sealed class AnomalyAutoInjectorVisualizerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UsedAnomalyAutoInjectorComponent, ComponentStartup>(OnUsedStartup);
        SubscribeLocalEvent<UsedAnomalyAutoInjectorComponent, ComponentShutdown>(OnUsedShutdown);
    }

    private void OnUsedStartup(EntityUid uid, UsedAnomalyAutoInjectorComponent comp, ComponentStartup args)
    {
        if (EntityManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
            sprite.LayerSetState(0, "anomagen_empty");
    }

    private void OnUsedShutdown(EntityUid uid, UsedAnomalyAutoInjectorComponent comp, ComponentShutdown args)
    {
        if (EntityManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
            sprite.LayerSetState(0, "anomagen");
    }
}
