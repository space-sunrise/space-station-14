using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.Execution;

public sealed partial class ExecutionSystem
{
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
