using System.Linq;
using Content.Shared._Sunrise.Footprints;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server._Sunrise.Footprints;

/// <summary>
/// Handles footprint creation when entities interact with puddles
/// </summary>
public sealed class PuddleFootprintSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PuddleFootprintComponent, EndCollideEvent>(OnPuddleInteraction);
    }

    /// <summary>
    /// Handles puddle interaction and footprint creation when entity exits the puddle
    /// </summary>
    private void OnPuddleInteraction(EntityUid uid, PuddleFootprintComponent component, ref EndCollideEvent args)
{
    if (!TryComp<AppearanceComponent>(uid, out var appearance)
        || !TryComp<PuddleComponent>(uid, out var puddle)
        || !TryComp<FootprintEmitterComponent>(args.OtherEntity, out var emitter)
        || !TryComp<SolutionContainerManagerComponent>(uid, out var solutionManager)
        || !_solutionSystem.ResolveSolution((uid, solutionManager), puddle.SolutionName, ref puddle.Solution, out var puddleSolutions)
        || !TryComp<SolutionContainerManagerComponent>(args.OtherEntity, out var emitterSolutionManager)
        || !_solutionSystem.ResolveSolution((args.OtherEntity, emitterSolutionManager), emitter.SolutionName, ref emitter.Solution, out var emitterSolutions))
        return;

    var totalSolutionQuantity = puddleSolutions.Contents.Sum(sol => (float)sol.Quantity);
    var waterQuantity = (from sol in puddleSolutions.Contents where sol.Reagent.Prototype == "Water" select (float)sol.Quantity).FirstOrDefault();

    if (waterQuantity / (totalSolutionQuantity / 100f) > component.WaterThresholdPercent || puddleSolutions.Contents.Count <= 0)
        return;

    var availableSpace = emitterSolutions.MaxVolume.Float() - emitterSolutions.Volume.Float();

    if (availableSpace <= 0)
        return;

    var transferVolume = Math.Min(component.TransferVolume, availableSpace);

    if (puddleSolutions.Volume < transferVolume)
        transferVolume = puddleSolutions.Volume.Float();

    if (transferVolume <= 0)
        return;

    var splitSolution = _solutionSystem.SplitSolution(puddle.Solution.Value, transferVolume);

    _solutionSystem.AddSolution(emitter.Solution.Value, splitSolution);
}
}
