using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Explosion.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;

namespace Content.Server._Sunrise.Execution;

public sealed partial class ExecutionSystem
{
    private static bool TryGetVerbContext(
        ref GetVerbsEvent<UtilityVerb> args,
        out EntityUid attacker,
        out EntityUid weapon,
        out EntityUid victim,
        out bool suicide)
    {
        attacker = default;
        weapon = default;
        victim = default;
        suicide = false;

        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return false;

        attacker = args.User;
        weapon = args.Using.Value;
        victim = args.Target;
        suicide = attacker == victim;
        return true;
    }

    private bool CanExecuteWithAny(EntityUid victim, EntityUid attacker)
    {
        // No point executing someone if they can't take damage
        if (!HasComp<DamageableComponent>(victim))
            return false;

        // You can't execute something that cannot die
        if (!TryComp<MobStateComponent>(victim, out var mobState))
            return false;

        // You can't execute borgs
        if (HasComp<BorgChassisComponent>(victim))
            return false;

        // You're not allowed to execute dead people (no fun allowed)
        if (_mobStateSystem.IsDead(victim, mobState))
            return false;

        // You must be able to attack people to execute
        if (!_actionBlockerSystem.CanAttack(attacker, victim))
            return false;

        // The victim must be incapacitated to be executed
        if (victim != attacker && _actionBlockerSystem.CanInteract(victim, null))
            return false;

        return true;
    }

    private bool CanExecuteWithMelee(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanExecuteWithAny(victim, user))
            return false;

        // We must be able to actually hurt people with the weapon
        if (!TryComp<MeleeWeaponComponent>(weapon, out var melee) || melee.Damage.GetTotal() <= 0.0f)
            return false;

        return true;
    }

    private bool CanExecuteWithGun(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanExecuteWithAny(victim, user))
            return false;

        // We must be able to actually fire the gun
        if (!TryComp<GunComponent>(weapon, out var gun) || !_gunSystem.CanShoot(gun))
            return false;

        if (TryComp<DamageableComponent>(victim, out var damageable))
        {
            if (TryComp<BatteryAmmoProviderComponent>(weapon, out var battery) &&
                !PrototypeHasLethalEffect(damageable, battery.Prototype))
            {
                return false;
            }

            if (HasNonLethalAmmoPrototype(weapon))
                return false;
        }

        return true;
    }

    private bool HasNonLethalAmmoPrototype(EntityUid weapon)
    {
        if (TryComp<BallisticAmmoProviderComponent>(weapon, out var ballistic) &&
            ballistic.Proto != null &&
            IsNonLethalAmmo(ballistic.Proto.Value))
        {
            return true;
        }

        if (TryComp<RevolverAmmoProviderComponent>(weapon, out var revolver) &&
            revolver.FillPrototype != null &&
            IsNonLethalAmmo(revolver.FillPrototype))
        {
            return true;
        }

        if (TryComp<BasicEntityAmmoProviderComponent>(weapon, out var basic) &&
            IsNonLethalAmmo(basic.Proto))
        {
            return true;
        }

        if (_containerSystem.TryGetContainer(weapon, GunMagazineContainerId, out var container) &&
            container is ContainerSlot slot &&
            slot.ContainedEntity is { } magEntity)
        {
            if (TryComp<BallisticAmmoProviderComponent>(magEntity, out var magBallistic) &&
                magBallistic.Proto != null &&
                IsNonLethalAmmo(magBallistic.Proto.Value))
            {
                return true;
            }

            if (TryComp<BasicEntityAmmoProviderComponent>(magEntity, out var magBasic) &&
                IsNonLethalAmmo(magBasic.Proto))
            {
                return true;
            }
        }

        return false;
    }

    // Чтобы не убивало от пустых патрон
    // Вероятно можно было сделать это более элегантно, но лень переделывать сейча. Сорян
    private bool PrototypeHasLethalEffect(DamageableComponent damageable, EntProtoId ammoPrototype)
    {
        var proto = _prototypeManager.Index<EntityPrototype>(ammoPrototype);

        if (proto.TryGetComponent<ExplosiveComponent>(out _, _componentFactory))
            return true;

        DamageSpecifier? damage = null;

        if (proto.TryGetComponent<ProjectileComponent>(out var projectile, _componentFactory) && projectile != null && !projectile.Damage.Empty)
            damage = projectile.Damage;
        else if (proto.TryGetComponent<HitscanBasicDamageComponent>(out var hitscan, _componentFactory) && hitscan != null && !hitscan.Damage.Empty)
            damage = hitscan.Damage;

        if (damage == null)
            return false;

        foreach (var (type, value) in damage.DamageDict)
        {
            if (value > FixedPoint2.Zero && damageable.Damage.DamageDict.ContainsKey(type))
                return true;
        }

        return false;
    }

    private static bool IsNonLethalAmmo(string? prototypeId)
    {
        if (string.IsNullOrWhiteSpace(prototypeId))
            return false;

        foreach (var token in NonLethalAmmoIdTokens)
        {
            if (prototypeId.Contains(token, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static DamageSpecifier FilterToSupportedDamage(DamageableComponent damageable, DamageSpecifier damage)
    {
        if (damage.Empty)
            return new DamageSpecifier();

        var filtered = new DamageSpecifier();

        foreach (var (type, value) in damage.DamageDict)
        {
            if (value <= FixedPoint2.Zero)
                continue;

            if (!damageable.Damage.DamageDict.ContainsKey(type))
                continue;

            filtered.DamageDict[type] = value;
        }

        return filtered;
    }

    private bool ApplyExecutionDamage(
        EntityUid victim,
        EntityUid weapon,
        DamageSpecifier baseDamage,
        bool forceLethal,
        float overkillFractionMin,
        float overkillFractionMax)
    {
        if (!TryComp<DamageableComponent>(victim, out var damageable))
            return false;

        var damage = FilterToSupportedDamage(damageable, baseDamage);
        if (damage.Empty || !damage.AnyPositive())
            return false;

        if (!forceLethal)
        {
            _damageableSystem.ChangeDamage(
                victim,
                damage,
                ignoreResistances: false,
                origin: weapon,
                ignoreVariance: true,
                ignoreGlobalModifiers: false);

            return true;
        }

        if (!TryComp<MobThresholdsComponent>(victim, out var thresholds))
            return false;

        damage.DamageDict.Remove(StructuralDamageType);
        if (damage.Empty || !damage.AnyPositive())
            return false;

        var lethalRemaining = thresholds.Thresholds.Keys.Last() - damageable.TotalDamage;
        if (lethalRemaining <= FixedPoint2.Zero)
            return true;

        var overkillFraction = _random.NextFloat(overkillFractionMin, overkillFractionMax);
        var overkill = lethalRemaining * overkillFraction;
        var totalToApply = lethalRemaining + overkill;

        var finalDamage = DistributeDamage(damage, totalToApply);
        if (finalDamage.Empty || !finalDamage.AnyPositive())
            return false;

        _damageableSystem.ChangeDamage(
            victim,
            finalDamage,
            ignoreResistances: true,
            origin: weapon,
            ignoreVariance: true,
            ignoreGlobalModifiers: true);

        return true;
    }

    private static DamageSpecifier DistributeDamage(DamageSpecifier weights, FixedPoint2 total)
    {
        if (total <= FixedPoint2.Zero)
            return new DamageSpecifier();

        var result = new DamageSpecifier(weights);
        var weightsTotal = result.GetTotal();
        if (weightsTotal <= FixedPoint2.Zero)
            return new DamageSpecifier();

        foreach (var type in result.DamageDict.Keys.ToArray())
        {
            var value = result.DamageDict[type];
            if (value <= FixedPoint2.Zero)
            {
                result.DamageDict.Remove(type);
                continue;
            }

            // Сделано по патерну SharedSuicideSystem
            result.DamageDict[type] = Math.Ceiling((double)(value * total / weightsTotal));
        }

        return result;
    }

    private void ShowExecutionPopup(string locString, Filter filter, PopupType type,
        EntityUid attacker, EntityUid victim, EntityUid weapon)
    {
        _popupSystem.PopupEntity(Loc.GetString(
                locString, ("attacker", attacker), ("victim", victim), ("weapon", weapon)),
            attacker, filter, true, type);
    }
}
