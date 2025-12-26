using System.Linq;
using Content.Shared._Sunrise.Flyswatter;
using Content.Shared.Body.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._Sunrise.Flyswatter;

public sealed class FlyswatterSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, AttackedEvent>(OnAttacked);
    }

    private void OnAttacked(EntityUid uid, BloodstreamComponent component, AttackedEvent args)
    {
        if (!TryComp(args.Used, out FlyswatterComponent? flyswatter))
            return;

        // множитель урона должен быть больше 1, иначе смысла нет
        if (flyswatter.InsectDamageMultiplier <= 1f)
            return;

        // BloodstreamComponent doesn't have a single BloodReagent field.
        // Inspect the bloodstream's reference solution contents for the reagent
        // prototype ID configured on the flyswatter. This avoids calling methods
        // on the Solution instance which may be restricted by component access.
        var contents = component.BloodReferenceSolution?.Contents;
        if (contents == null || !contents.Any(q => q.Reagent.Prototype == flyswatter.InsectBloodReagent))
            return;

        // 0 смысла если не бьет
        if (!TryComp(args.Used, out MeleeWeaponComponent? meleeWeapon) || meleeWeapon.Damage.Empty)
            return;

        // К базовому урону добавляем бонусный урон, 1f это уже учтенный базовый урон
        args.BonusDamage += meleeWeapon.Damage * (flyswatter.InsectDamageMultiplier - 1f);
    }
}
