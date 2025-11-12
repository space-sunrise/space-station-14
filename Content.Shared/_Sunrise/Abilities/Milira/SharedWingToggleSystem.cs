using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Robust.Shared.Localization;

namespace Content.Shared._Sunrise.Abilities.Milira;

public sealed class SharedWingToggleSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WingToggleComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<WingToggleComponent, IsEquippingTargetAttemptEvent>(OnEquipTargetAttempt);
    }

    private void OnEquipAttempt(EntityUid uid, WingToggleComponent component, ref IsEquippingAttemptEvent args)
    {
        if (!component.WingsOpened || args.Slot != "outerClothing")
            return;

        args.Cancel();
        args.Reason = "action-wing-toggle-equip-blocked";

        ShowEquipBlockedPopup(uid, args.Equipee);
    }

    private void OnEquipTargetAttempt(EntityUid uid, WingToggleComponent component, ref IsEquippingTargetAttemptEvent args)
    {
        if (!component.WingsOpened || args.Slot != "outerClothing")
            return;

        args.Cancel();
        args.Reason = "action-wing-toggle-equip-blocked";

        ShowEquipBlockedPopup(uid, args.Equipee);
    }

    public void ShowEquipBlockedPopup(EntityUid uid, EntityUid equipee)
    {
        var message = Loc.GetString("wing-toggle-equip-blocked");
        _popup.PopupEntity(message, uid, uid, PopupType.Medium);

        if (equipee != uid)
            _popup.PopupEntity(message, equipee, equipee, PopupType.Medium);
    }

    public void ShowOpenBlockedPopup(EntityUid uid)
    {
        var message = Loc.GetString("wing-toggle-open-blocked");
        _popup.PopupEntity(message, uid, uid, PopupType.Medium);
    }
}
