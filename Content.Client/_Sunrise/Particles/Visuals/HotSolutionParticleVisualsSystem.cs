using Content.Shared._Sunrise.Particles;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// Drives persistent steam from the synchronized volume and temperature of a managed solution.
/// </summary>
public sealed class HotSolutionParticleVisualsSystem : EntitySystem
{
    [Dependency] private readonly ParticleOrchestraSystem _orchestra = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private const float MinimumIntensity = 0.45f;
    private const float IntensityRange = 0.85f;

    private readonly Dictionary<EntityUid, ActiveParticleOrchestra> _active = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HotSolutionParticleVisualsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<HotSolutionParticleVisualsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HotSolutionParticleVisualsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HotSolutionParticleVisualsComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
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

    private void OnStartup(Entity<HotSolutionParticleVisualsComponent> ent, ref ComponentStartup args)
    {
        Refresh(ent);
    }

    private void OnMapInit(Entity<HotSolutionParticleVisualsComponent> ent, ref MapInitEvent args)
    {
        Refresh(ent);
    }

    private void OnShutdown(Entity<HotSolutionParticleVisualsComponent> ent, ref ComponentShutdown args)
    {
        Stop(ent);
    }

    private void OnSolutionChanged(
        Entity<HotSolutionParticleVisualsComponent> ent,
        ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != ent.Comp.Solution)
            return;

        Refresh(ent, args.Solution);
    }

    private void Refresh(Entity<HotSolutionParticleVisualsComponent> ent, Solution? solution = null)
    {
        if (solution == null &&
            !_solution.TryGetSolution((ent.Owner, null), ent.Comp.Solution, out _, out solution))
        {
            Stop(ent);
            return;
        }

        if (solution.Volume.Float() <= 0f || solution.Temperature < ent.Comp.MinimumTemperature)
        {
            Stop(ent);
            return;
        }

        var temperatureRange = Math.Max(
            0.01f,
            ent.Comp.FullIntensityTemperature - ent.Comp.MinimumTemperature);
        var temperatureFactor = Math.Clamp(
            (solution.Temperature - ent.Comp.MinimumTemperature) / temperatureRange,
            0f,
            1f);
        var intensity = MinimumIntensity + temperatureFactor * IntensityRange;

        if (_active.TryGetValue(ent, out var active))
        {
            _orchestra.UpdateIntensity(active, intensity);
            return;
        }

        if (_orchestra.Start(ent.Comp.Orchestra, ent, intensity: intensity) is { } orchestra)
            _active.Add(ent, orchestra);
    }

    private void Stop(EntityUid uid)
    {
        if (!_active.Remove(uid, out var orchestra))
            return;

        _orchestra.Stop(orchestra);
    }
}
