using System.Linq;
using Content.Shared._Sunrise.Footprints;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids;
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
            || !_solutionSystem.ResolveSolution((uid, solutionManager), puddle.SolutionName, ref puddle.Solution, out var solutions))
            return;

        var totalSolutionQuantity = solutions.Contents.Sum(sol => (float)sol.Quantity);
        var waterQuantity = (from sol in solutions.Contents where sol.Reagent.Prototype == "Water" select (float)sol.Quantity).FirstOrDefault();

        if (waterQuantity / (totalSolutionQuantity / 100f) > component.WaterThresholdPercent || solutions.Contents.Count <= 0)
            return;

        emitter.CurrentReagent = solutions.Contents.Aggregate((l, r) => l.Quantity > r.Quantity ? l : r).Reagent.Prototype;

        if (_appearanceSystem.TryGetData(uid, PuddleVisuals.SolutionColor, out var color, appearance)
            && _appearanceSystem.TryGetData(uid, PuddleVisuals.CurrentVolume, out var volume, appearance))
            UpdateTrackColor((Color)color, (float)volume * component.ColorTransferRatio, emitter);

        _solutionSystem.RemoveEachReagent(puddle.Solution.Value, 1);
    }

    /// <summary>
    /// Updates the color of footprints based on puddle properties
    /// </summary>
    private void UpdateTrackColor(Color color, float quantity, FootprintEmitterComponent emitter)
    {
        emitter.TrackColor = emitter.AccumulatedColor == 0f ? color : Color.InterpolateBetween(emitter.TrackColor, color, emitter.ColorBlendFactor);
        emitter.AccumulatedColor += quantity;
    }
}
