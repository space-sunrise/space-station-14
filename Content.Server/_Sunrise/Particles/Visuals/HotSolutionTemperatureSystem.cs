using Content.Shared._Sunrise.Particles;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;

namespace Content.Server._Sunrise.Particles.Visuals;

/// <summary>
/// Keeps the managed solution temperature in sync with items heated through <see cref="TemperatureComponent"/>.
/// </summary>
public sealed class HotSolutionTemperatureSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HotSolutionParticleVisualsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HotSolutionParticleVisualsComponent, OnTemperatureChangeEvent>(OnTemperatureChanged);
    }

    private void OnMapInit(Entity<HotSolutionParticleVisualsComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<TemperatureComponent>(ent, out var temperature))
            return;

        TrySetTemperature(ent, temperature.CurrentTemperature, onlyIfWarmer: true);
    }

    private void OnTemperatureChanged(
        Entity<HotSolutionParticleVisualsComponent> ent,
        ref OnTemperatureChangeEvent args)
    {
        TryShiftTemperature(ent, args.TemperatureDelta);
    }

    private void TrySetTemperature(
        Entity<HotSolutionParticleVisualsComponent> ent,
        float temperature,
        bool onlyIfWarmer = false)
    {
        TryUpdateTemperature(ent, temperature, relative: false, onlyIfWarmer: onlyIfWarmer);
    }

    private void TryShiftTemperature(
        Entity<HotSolutionParticleVisualsComponent> ent,
        float temperatureDelta)
    {
        TryUpdateTemperature(ent, temperatureDelta, relative: true);
    }

    private void TryUpdateTemperature(
        Entity<HotSolutionParticleVisualsComponent> ent,
        float value,
        bool relative,
        bool onlyIfWarmer = false)
    {
        if (!_solution.TryGetSolution(
                (ent.Owner, null),
                ent.Comp.Solution,
                out var solutionEntity,
                out var solution))
        {
            return;
        }

        if (solution.Volume.Float() <= 0f)
            return;

        var temperature = relative
            ? Math.Max(0f, solution.Temperature + value)
            : value;
        if (onlyIfWarmer && temperature <= solution.Temperature)
            return;

        _solution.SetTemperature(solutionEntity.Value, temperature);
    }
}
