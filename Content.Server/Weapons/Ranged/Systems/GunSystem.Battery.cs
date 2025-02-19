using Content.Server.Power.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeBattery()
    {
        base.InitializeBattery();

        // Hitscan
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, DamageExamineEvent>(OnBatteryDamageExamine);

        // Projectile
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, DamageExamineEvent>(OnBatteryDamageExamine);
    }

    private void OnBatteryStartup(EntityUid uid, BatteryAmmoProviderComponent component, ComponentStartup args)
    {
        UpdateShots(uid, component);
    }

    private void OnBatteryChargeChange(EntityUid uid, BatteryAmmoProviderComponent component, ref ChargeChangedEvent args)
    {
        UpdateShots(uid, component, args.Charge, args.MaxCharge);
    }

    private void UpdateShots(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        if (!TryComp<BatteryComponent>(uid, out var battery))
            return;

        UpdateShots(uid, component, battery.CurrentCharge, battery.MaxCharge);
    }

    private void UpdateShots(EntityUid uid, BatteryAmmoProviderComponent component, float charge, float maxCharge)
    {
        var shots = (int) (charge / component.FireCost);
        var maxShots = (int) (maxCharge / component.FireCost);

        if (component.Shots != shots || component.Capacity != maxShots)
        {
            Dirty(uid, component);
        }

        component.Shots = shots;
        component.Capacity = maxShots;
        UpdateBatteryAppearance(uid, component);
    }

    private void OnBatteryDamageExamine(EntityUid uid, BatteryAmmoProviderComponent component, ref DamageExamineEvent args)
    {
        var damageSpec = GetDamage(component);

        if (damageSpec == null)
            return;

        string damageType;
        var shotCount = 1;
        var shootModifier = ShootModifier.None;

        switch (component)
        {
            case HitscanBatteryAmmoProviderComponent hitscan:
                var hitScanPrototype = _proto.Index<HitscanPrototype>(hitscan.Prototype);
                if (hitScanPrototype.ShootModifier == ShootModifier.Split)
                {
                    shotCount = hitScanPrototype.SplitCount;
                    shootModifier = ShootModifier.Split;
                }
                else if (hitScanPrototype.ShootModifier == ShootModifier.Spread)
                {
                    shotCount = hitScanPrototype.SpreadCount;
                    shootModifier = ShootModifier.Spread;
                }

                damageType = Loc.GetString("damage-hitscan");
                break;
            case ProjectileBatteryAmmoProviderComponent projectile:
                var prototype = _proto.Index<EntityPrototype>(projectile.Prototype);
                if (prototype.TryGetComponent<ProjectileSpreadComponent>(out var ammoSpreadComp, _componentFactory))
                {
                    shotCount = ammoSpreadComp.Count;
                    shootModifier = ShootModifier.Spread;
                }

                damageType = Loc.GetString("damage-projectile");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _damageExamine.AddDamageExamineWithModifier(args.Message, Damageable.ApplyUniversalAllModifiers(damageSpec), shotCount, shootModifier, damageType);
    }

    private DamageSpecifier? GetDamage(BatteryAmmoProviderComponent component)
    {
        if (component is ProjectileBatteryAmmoProviderComponent battery)
        {
            if (ProtoManager.Index<EntityPrototype>(battery.Prototype).Components
                .TryGetValue(_factory.GetComponentName(typeof(ProjectileComponent)), out var projectile))
            {
                var p = (ProjectileComponent) projectile.Component;

                if (!p.Damage.Empty)
                {
                    return p.Damage * Damageable.UniversalProjectileDamageModifier;
                }
            }

            return null;
        }

        if (component is HitscanBatteryAmmoProviderComponent hitscan)
        {
            var dmg = ProtoManager.Index<HitscanPrototype>(hitscan.Prototype).Damage;
            return dmg == null ? dmg : dmg * Damageable.UniversalHitscanDamageModifier;
        }

        return null;
    }

    protected override void TakeCharge(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        // Will raise ChargeChangedEvent
        _battery.UseCharge(uid, component.FireCost);
    }
}
