using Content._Sunrise.Shared.Shower;
using Content.Shared.Interaction;

namespace Content._Sunrise.Server.Shower;

public sealed class ShowerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private float _accumulator = 0f;
    private const float UpdateInterval = 10f;
    private const string PuddlePrototype = "PuddleWater";
    private EntityUid? _currentPuddle;

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
        if (_currentPuddle == null || Deleted(_currentPuddle))
        {
            _currentPuddle = Spawn(PuddlePrototype, Transform(ent).Coordinates);
        }
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

            Spawn(PuddlePrototype, Transform(uid).Coordinates);
        }
    }
}