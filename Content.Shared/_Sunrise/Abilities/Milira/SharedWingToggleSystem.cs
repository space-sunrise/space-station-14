using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared._Sunrise.Abilities.Milira;
using Robust.Shared.Localization;

namespace Content.Shared._Sunrise.Abilities.Milira;

/// <summary>
/// Shared система для блокировки одевания одежды при раскрытых крыльях.
/// </summary>
public sealed class SharedWingToggleSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WingToggleComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
    }

    private void OnEquipAttempt(EntityUid uid, WingToggleComponent component, ref IsEquippingAttemptEvent args)
    {
        if (!component.WingsOpened)
            return;

        if (args.Slot != "outerClothing")
            return;

        args.Cancel();
        args.Reason = "action-wing-toggle-equip-blocked";

        var message = Loc.GetString("wing-toggle-equip-blocked");
        _popup.PopupEntity(message, uid, uid, PopupType.Medium);

        if (args.Equipee != uid)
            _popup.PopupEntity(message, args.Equipee, args.Equipee, PopupType.Medium);
    }
}

