using Content.Server.Fluids.EntitySystems;
using Content._Sunrise.Shared.Shower;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;

namespace Content._Sunrise.Server.Shower;

public sealed class ShowerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    private float _accumulator = 0f;
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

        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;
        if (_accumulator < UpdateInterval)
            return;

        _accumulator -= UpdateInterval;

        var query = EntityQueryEnumerator<ShowerComponent>();
        while (query.MoveNext(out var uid, out var shower))
        {
            if (!shower.IsActive)
                continue;

            SpillWater((uid, shower));
        }
    }

    private void SpillWater(Entity<ShowerComponent> ent)
    {
        _puddle.TrySpillAt(ent, new Solution(ShowerReagent, ShowerSpillAmount), out _);
    }
}