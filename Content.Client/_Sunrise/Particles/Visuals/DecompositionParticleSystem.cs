using Content.Shared._Sunrise.Particles;
using Content.Shared.Atmos.Rotting;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// Adds subtle gas and fly ambience to entities that have entered the rotting state.
/// </summary>
public sealed class DecompositionParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleOrchestraSystem _orchestra = default!;

    private static readonly ProtoId<ParticleOrchestraPrototype> DecompositionOrchestra = "DecompositionAmbient";

    private readonly Dictionary<EntityUid, ActiveParticleOrchestra> _active = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RottingComponent, ComponentStartup>(OnStartup);
        // ComponentShutdown уже занят SharedRottingSystem, поэтому визуал очищается на финальном удалении компонента.
        SubscribeLocalEvent<RottingComponent, ComponentRemove>(OnRemove);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        foreach (var orchestra in _active.Values)
        {
            _orchestra.Stop(orchestra);
        }

        _active.Clear();
    }

    private void OnStartup(Entity<RottingComponent> ent, ref ComponentStartup args)
    {
        if (_active.ContainsKey(ent))
            return;

        if (_orchestra.Start(DecompositionOrchestra, ent) is { } orchestra)
            _active.Add(ent, orchestra);
    }

    private void OnRemove(Entity<RottingComponent> ent, ref ComponentRemove args)
    {
        if (!_active.Remove(ent, out var orchestra))
            return;

        _orchestra.Stop(orchestra);
    }
}
