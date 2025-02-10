using Content.Shared.IdentityManagement;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Felinid;

public sealed class SharedFelinidSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FelinidComponent, PickupAttemptEvent>(OnPickupAttempt);
    }

    private void OnPickupAttempt(EntityUid uid, FelinidComponent component, PickupAttemptEvent args)
    {
        if (!HasComp<FelinidComponent>(args.Item))
            return;

        ShowPopup((uid, component), args.Item, "Коллапс неизбежен");
        args.Cancel();
    }

    private void ShowPopup(Entity<FelinidComponent> user, EntityUid target, string reason)
    {
        if (!(_timing.CurTime > user.Comp.NextPopupTime))
            return;

        var targetName = Identity.Entity(target, EntityManager);
        _popup.PopupCursor(Loc.GetString(reason, ("entity", targetName)), user);
        user.Comp.NextPopupTime = _timing.CurTime + user.Comp.PopupCooldown;
    }
}
