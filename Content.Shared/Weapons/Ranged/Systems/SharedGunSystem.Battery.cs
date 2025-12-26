using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Projectiles;
using Content.Shared.Power;
using Content.Shared.PowerCell;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    protected virtual void InitializeBattery()
    {
        SubscribeLocalEvent<BatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, TakeAmmoEvent>(OnBatteryTakeAmmo);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, GetAmmoCountEvent>(OnBatteryAmmoCount);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, ExaminedEvent>(OnBatteryExamine);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, DamageExamineEvent>(OnBatteryDamageExamine);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, ChargeChangedEvent>(OnChargeChanged);
    }

    private void OnBatteryExamine(Entity<BatteryAmmoProviderComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("gun-battery-examine", ("color", AmmoExamineColor), ("count", ent.Comp.Shots)));
    }

    private void OnBatteryDamageExamine(Entity<BatteryAmmoProviderComponent> ent, ref DamageExamineEvent args)
    {
        var proto = ProtoManager.Index<EntityPrototype>(ent.Comp.Prototype);
        DamageSpecifier? damageSpec = null;
        var damageType = string.Empty;

        if (proto.TryGetComponent<ProjectileComponent>(out var projectileComp, Factory))
        {
            if (!projectileComp.Damage.Empty)
            {
                damageType = Loc.GetString("damage-projectile");
                damageSpec = projectileComp.Damage * Damageable.UniversalProjectileDamageModifier;
            }
        }
        else if (proto.TryGetComponent<HitscanBasicDamageComponent>(out var hitscanComp, Factory))
        {
            if (!hitscanComp.Damage.Empty)
            {
                damageType = Loc.GetString("damage-hitscan");
                damageSpec = hitscanComp.Damage * Damageable.UniversalHitscanDamageModifier;
            }
        }
        if (damageSpec == null)
            return;

        _damageExamine.AddDamageExamine(args.Message, Damageable.ApplyUniversalAllModifiers(damageSpec), damageType);
    }

    private void OnBatteryTakeAmmo(Entity<BatteryAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        var shots = Math.Min(args.Shots, ent.Comp.Shots);

            return null;
        }

        if (component is HitscanBatteryAmmoProviderComponent hitscan)
        {
            var dmg = ProtoManager.Index<HitscanPrototype>(hitscan.Prototype).Damage;
            return dmg == null ? dmg : dmg * Damageable.UniversalHitscanDamageModifier;
        }

        return null;
    }

    private void OnBatteryTakeAmmo(EntityUid uid, BatteryAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var shots = Math.Min(args.Shots, component.Shots);

        // Don't dirty if it's an empty fire.
        if (shots == 0)
            return;

        for (var i = 0; i < shots; i++)
        {
            args.Ammo.Add(GetShootable(ent, args.Coordinates));
        }

        TakeCharge(ent, shots);
    }

    private void OnBatteryAmmoCount(Entity<BatteryAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        args.Count = ent.Comp.Shots;
        args.Capacity = ent.Comp.Capacity;
    }

    /// <summary>
    /// Use up the required amount of battery charge for firing.
    /// </summary>
    public void TakeCharge(Entity<BatteryAmmoProviderComponent> ent, int shots = 1)
    {
        // Take charge from either the BatteryComponent or PowerCellSlotComponent.
        var ev = new ChangeChargeEvent(-ent.Comp.FireCost * shots);
        RaiseLocalEvent(ent, ref ev);
        // UpdateShots is already called by the resulting ChargeChangedEvent
    }

    private (EntityUid? Entity, IShootable) GetShootable(BatteryAmmoProviderComponent component, EntityCoordinates coordinates)
    {
        switch (component)
        {
            case ProjectileBatteryAmmoProviderComponent proj:
                var ent = Spawn(proj.Prototype, coordinates);
                return (ent, EnsureShootable(ent));
            case HitscanBatteryAmmoProviderComponent hitscan:
                return (null, ProtoManager.Index<HitscanPrototype>(hitscan.Prototype));
            default:
                throw new ArgumentOutOfRangeException();
        }
        else if (currentChargeRate < 0f && currentCharge != 0f)
        {
            ent.Comp.NextUpdate = Timing.CurTime + TimeSpan.FromSeconds(-(currentCharge % ent.Comp.FireCost) / currentChargeRate);
            ent.Comp.ChargeTime = TimeSpan.FromSeconds(-ent.Comp.FireCost / currentChargeRate);
        }
        Dirty(ent);
    }

    // Shots are only chached, not a DataField, so we need to refresh this when the game is loaded.
    private void OnBatteryStartup(Entity<BatteryAmmoProviderComponent> ent, ref ComponentStartup args)
    {
        if (_netManager.IsClient && !IsClientSide(ent.Owner))
            return; // Don't overwrite the server state in cases where the battery is not predicted.

        UpdateShots(ent);
    }

    /// <summary>
    /// Gets the current and maximum amount of shots from this entity's battery.
    /// This works for BatteryComponent and PowercellSlotComponent.
    /// </summary>
    public (int, int) GetShots(Entity<BatteryAmmoProviderComponent> ent)
    {
        var ev = new GetChargeEvent();
        RaiseLocalEvent(ent, ref ev);
        var currentShots = (int)(ev.CurrentCharge / ent.Comp.FireCost);
        var maxShots = (int)(ev.MaxCharge / ent.Comp.FireCost);

        return (currentShots, maxShots);
    }

    /// <summary>
    /// Update loop for refreshing the ammo counter for charging/draining batteries.
    /// </summary>
    private void UpdateBattery(float frameTime)
    {
        var curTime = Timing.CurTime;
        var hitscanQuery = EntityQueryEnumerator<BatteryAmmoProviderComponent>();
        while (hitscanQuery.MoveNext(out var uid, out var provider))
        {
            if (provider.NextUpdate == null || curTime < provider.NextUpdate)
                continue;
            UpdateShots((uid, provider));
            provider.NextUpdate += provider.ChargeTime; // Queue another update for when we reach the next full charge.
            Dirty(uid, provider);
            // TODO: Stop updating when full or empty.
        }
    }
}
