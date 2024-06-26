using Content.Server.FootPrints.Components;
using Content.Shared.Fluids.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.FootPrints;

public sealed class PuddleFootPrintsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PuddleFootPrintsComponent, EndCollideEvent>(OnStepTrigger);
    }

    private void OnStepTrigger(EntityUid uid, PuddleFootPrintsComponent comp, ref EndCollideEvent args)
    {
        if (!TryComp<FootPrintsComponent>(args.OtherEntity, out var footPrints))
            return;

        if (!TryComp<PuddleComponent>(uid, out var puddle) || puddle.Solution is not { } entSolution)
            return;


        var solution = entSolution.Comp.Solution;
        var quantity = solution.Volume;
        var color = solution.GetColor(_prototype);

        footPrints.PrintsColor = footPrints.ColorQuantity == 0f
            ? color
            : Color.InterpolateBetween(footPrints.PrintsColor, color, 0.3f);
        footPrints.ColorQuantity += quantity.Float() * 1.2f;
    }

}
