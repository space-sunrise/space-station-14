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
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

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
            _popupSystem.PopupEntity("Вы не можете заточить это", target, args.User);
            return;
        }

        if (!TryComp<MeleeWeaponComponent>(target, out var meleeWeaponComponent))
        {
            _popupSystem.PopupEntity("Вы не можете заточить это", target, args.User);
            return;
        }

        if (!meleeWeaponComponent.Damage.DamageDict.ContainsKey("Slash"))
        {
            _popupSystem.PopupEntity("У оружия должно быть остреё", target, args.User);
            return;
        }

        if (HasComp<SharpenedComponent>(target))
        {
            _popupSystem.PopupEntity("Клинок уже заточен", target, args.User);
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

        _popupSystem.PopupEntity("Клинок успешно заточен", target, args.User);
    }

    private void OnMeleeHit(EntityUid uid, SharpenedComponent component, MeleeHitEvent args)
    {
        component.AttacksLeft--;

        if (component.AttacksLeft == 10)
        {
            _popupSystem.PopupEntity("Клинок начал затупляться", uid, args.User);
        }

        if (component.AttacksLeft > 0)
            return;

        _popupSystem.PopupEntity("Клинок потерял свою заточку", uid, args.User);
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
