using Content.Shared.Item;

namespace Content.Shared._Sunrise.Felinid;

public sealed class SharedFelinidSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FelinidComponent, PickupAttemptEvent>(OnPickupAttempt);
    }

    private void OnPickupAttempt(EntityUid uid, FelinidComponent component, PickupAttemptEvent args)
    {
        if (!HasComp<FelinidComponent>(args.Item))
            return;

        args.Cancel();
    }
}
