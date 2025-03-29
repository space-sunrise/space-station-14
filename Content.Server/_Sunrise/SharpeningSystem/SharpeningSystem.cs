using Content.Shared._Sunrise.BloodCult.Items;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._Sunrise.SharpeningSystem;

public sealed class SharpeningSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharpenerComponent, AfterInteractEvent>(OnSharping);

        SubscribeLocalEvent<SharpenedComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnSharping(EntityUid uid, SharpenerComponent component, AfterInteractEvent args)
    {
        if (!args.Target.HasValue)
            return;

        var target = args.Target.Value;

        if (!HasComp<ItemComponent>(target))
        {
            _popupSystem.PopupEntity(Loc.GetString("sharpening-failed"), target, args.User);
            return;
        }

        if (!TryComp<MeleeWeaponComponent>(target, out var meleeWeaponComponent))
        {
            _popupSystem.PopupEntity(Loc.GetString("sharpening-failed"), target, args.User);
            return;
        }

        if (!meleeWeaponComponent.Damage.DamageDict.ContainsKey("Slash"))
        {
            _popupSystem.PopupEntity(Loc.GetString("sharpening-failed-blade"), target, args.User);
            return;
        }

        if (HasComp<SharpenedComponent>(target))
        {
            _popupSystem.PopupEntity(Loc.GetString("sharpening-failed-double"), target, args.User);
            return;
        }

        if (component.Usages <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("sharpening-used"), target, args.User);
            return;
        }

        EnsureComp<SharpenedComponent>(target).DamageBonus = component.DamageBonus;

        component.Usages -= 1;

        _popupSystem.PopupEntity(Loc.GetString("sharpening-success"), target, args.User);

        if (component.Usages > 0)
            return;

        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, SharpenerVisuals.Visual, SharpenerVisuals.Used, appearance);
    }

    private void OnMeleeHit(EntityUid uid, SharpenedComponent component, MeleeHitEvent args)
    {
        args.BonusDamage += component.DamageBonus;
        component.AttacksLeft--;

        if (component.AttacksLeft == 10)
        {
            _popupSystem.PopupEntity(Loc.GetString("sharpening-roughing-begin"), uid, args.User);
        }

        if (component.AttacksLeft > 0)
            return;

        _popupSystem.PopupEntity(Loc.GetString("sharpening-removed"), uid, args.User);
        RemCompDeferred<SharpenedComponent>(uid);
    }
}
