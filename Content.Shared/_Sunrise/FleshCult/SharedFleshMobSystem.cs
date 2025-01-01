using Content.Shared.Flesh;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Shared._Sunrise.FleshCult;

public abstract class SharedFleshMobSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FleshMobComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    private void OnAttackAttempt(EntityUid uid, FleshMobComponent component, AttackAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasComp<FleshMobComponent>(args.Target))
        {
            _popup.PopupCursor(Loc.GetString("flesh-mob-cant-atack-flesh-mob"), uid,
                PopupType.LargeCaution);
            args.Cancel();
        }
        if (HasComp<_Sunrise.FleshCult.FleshCultistComponent>(args.Target))
        {
            _popup.PopupCursor(Loc.GetString("flesh-mob-cant-atack-flesh-cultist"), uid,
                PopupType.LargeCaution);
            args.Cancel();
        }

        if (HasComp<FleshHeartComponent>(args.Target))
        {
            _popup.PopupCursor(Loc.GetString("flesh-mob-cant-atack-flesh-heart"), uid,
                PopupType.LargeCaution);
            args.Cancel();
        }
    }

}
