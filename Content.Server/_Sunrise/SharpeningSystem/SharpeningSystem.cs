using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.SharpeningSystem;

public sealed class SharpeningSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharpenerComponent, AfterInteractEvent>(OnSharping);

        SubscribeLocalEvent<SharpenedComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<SharpenedComponent, ComponentRemove>(OnSharpenedComponentRemove);
    }

    private void OnSharping(EntityUid uid, SharpenerComponent component, AfterInteractEvent args)
    {
        if (!args.Target.HasValue)
            return;

        var target = args.Target.Value;

        if (!TryComp<ItemComponent>(target, out _))
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

        EnsureComp<SharpenedComponent>(target).DamageModifier = component.DamageModifier;

        meleeWeaponComponent.Damage.ExclusiveAdd(
            new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"), component.DamageModifier));

        component.Usages -= 1;

        if (component.Usages <= 0)
        {
            Del(uid);
        }

        _popupSystem.PopupEntity(Loc.GetString("sharpening-success"), target, args.User);
    }

    private void OnMeleeHit(EntityUid uid, SharpenedComponent component, MeleeHitEvent args)
    {
        component.AttacksLeft--;

        if (component.AttacksLeft == 10)
        {
            _popupSystem.PopupEntity(Loc.GetString("sharpening-roughing-begin"), uid, args.User);
        }

        if (component.AttacksLeft > 0)
            return;

        _popupSystem.PopupEntity(Loc.GetString("sharpening-removed"), uid, args.User);
        RemComp(uid, component);
    }

    private void OnSharpenedComponentRemove(EntityUid uid, SharpenedComponent component, ComponentRemove args)
    {
        if (!TryComp(uid, out MeleeWeaponComponent? meleeWeapon))
        {
            return;
        }

        meleeWeapon.Damage.ExclusiveAdd(
            new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"), -component.DamageModifier));
    }
}
