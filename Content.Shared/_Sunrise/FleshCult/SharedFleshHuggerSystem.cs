using Content.Shared._Sunrise.FleshCult;
using Content.Shared.Actions;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;

namespace Content.Shared._Sunrise.FleshCult;

public sealed class SharedFleshHuggerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FleshHuggerComponent, BeingUnequippedAttemptEvent>(OnUnequipAttempt);
    }

    private void OnUnequipAttempt(EntityUid uid, FleshHuggerComponent component, BeingUnequippedAttemptEvent args)
    {
        if (args.Slot != "mask")
            return;
        if (component.EquipedOn != args.Unequipee)
            return;
        if (HasComp<FleshCultistComponent>(args.Unequipee))
            return;
        _popup.PopupEntity(Loc.GetString("flesh-pudge-throw-hugger-try-unequip"),
            args.Unequipee,
            args.Unequipee,
            PopupType.Large);
        args.Cancel();
    }
}

public sealed partial class FleshHuggerJumpActionEvent : WorldTargetActionEvent
{

};

public sealed partial class FleshHuggerGetOffFromFaceActionEvent : InstantActionEvent
{

};
