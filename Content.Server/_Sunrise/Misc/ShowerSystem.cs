using Content.Shared._Sunrise.Shower;
using Content.Shared.Interaction;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Server._Sunrise.Shower;

public sealed class ShowerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private const float UpdateInterval = 5f;
    private const float ShowerFillDuration = 120f;
    private const float ShowerFillLimit = 200f;
    private const string ShowerReagent = "Water";
    private static readonly FixedPoint2 ShowerSpillAmount = FixedPoint2.New(ShowerFillLimit / (ShowerFillDuration / UpdateInterval));

    public override void Initialize()
    {
        SubscribeLocalEvent<ShowerComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnInteractHand(Entity<ShowerComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ent.Comp.IsActive = !ent.Comp.IsActive;
        _appearance.SetData(ent.Owner, ShowerVisuals.Active, ent.Comp.IsActive);
        ent.Comp.Accumulator = 0f; // сбрасываем таймер при переключении

        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShowerComponent>();
        while (query.MoveNext(out var uid, out var shower))
        {
            if (!shower.IsActive)
                continue;

            shower.Accumulator += frameTime;
            if (shower.Accumulator < UpdateInterval)
                continue;

            shower.Accumulator -= UpdateInterval;
            SpillWater((uid, shower));
            Dirty(uid, shower);
        }
    }

    private void SpillWater(Entity<ShowerComponent> ent)
    {
        var solution = new Solution(ShowerReagent, ShowerSpillAmount);
        _puddle.TrySpillAt(ent, solution, out _);
    }
}