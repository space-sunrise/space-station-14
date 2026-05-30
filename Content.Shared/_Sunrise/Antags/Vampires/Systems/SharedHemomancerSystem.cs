using Content.Shared._Sunrise.Antags.Vampires.Components.Classes;

namespace Content.Shared._Sunrise.Antags.Vampires.Systems;

public sealed class SharedHemomancerSystem : EntitySystem
{
    [Dependency] private readonly SharedVampireActionUseSystem _vampireActions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireHemomancerClawsActionEvent>(OnHemomancerClaws);
    }

    private void OnHemomancerClaws(VampireHemomancerClawsActionEvent args)
    {
        if (args.Handled || !TryActivateHemomancerClaws(args.Performer, args.Action.Owner))
            return;

        args.Handled = true;
    }

    public bool TryActivateHemomancerClaws(EntityUid uid, EntityUid action)
    {
        if (!Exists(action) || !_vampireActions.TryUse(uid, action))
            return false;

        if (TryComp<HemomancerComponent>(uid, out var hemomancer))
        {
            hemomancer.HemomancerClawsActive = true;
            Dirty(uid, hemomancer);
        }

        var activated = new VampireHemomancerClawsActivatedEvent(uid);
        RaiseLocalEvent(uid, ref activated, true);
        return true;
    }
}
