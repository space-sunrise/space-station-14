using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeCartridge()
    {
        base.InitializeCartridge();
        SubscribeLocalEvent<CartridgeAmmoComponent, ExaminedEvent>(OnCartridgeExamine);
        SubscribeLocalEvent<CartridgeAmmoComponent, DamageExamineEvent>(OnCartridgeDamageExamine);
        SubscribeLocalEvent<HitScanCartridgeAmmoComponent, ExaminedEvent>(OnHitScanCartridgeExamine);
        SubscribeLocalEvent<HitScanCartridgeAmmoComponent, DamageExamineEvent>(OnHitScanCartridgeDamageExamine);
    }

    private void OnHitScanCartridgeDamageExamine(EntityUid uid, HitScanCartridgeAmmoComponent component, ref DamageExamineEvent args)
    {
        var damageSpec = GetHitscanDamage(component.Prototype);

        if (damageSpec == null)
            return;

        var shotCount = 1;
        var shootModifier = ShootModifier.None;
        var hitScanPrototype = _proto.Index<HitscanPrototype>(component.Prototype);
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

        _damageExamine.AddDamageExamineWithModifier(args.Message, damageSpec, shotCount, shootModifier, Loc.GetString("damage-hitscan"));
    }

    private DamageSpecifier? GetHitscanDamage(string proto)
    {
        if (!ProtoManager.TryIndex<HitscanPrototype>(proto, out var hitscanProto))
            return null;

        return hitscanProto.Damage ?? null;
    }

    private void OnHitScanCartridgeExamine(EntityUid uid, HitScanCartridgeAmmoComponent component, ExaminedEvent args)
    {
        if (component.Spent)
        {
            args.PushMarkup(Loc.GetString("gun-cartridge-spent"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("gun-cartridge-unspent"));
        }
    }

    private void OnCartridgeDamageExamine(EntityUid uid, CartridgeAmmoComponent component, ref DamageExamineEvent args)
    {
        var damageSpec = GetProjectileDamage(component.Prototype);

        if (damageSpec == null)
            return;

        var shotCount = 1;
        var shootModifier = ShootModifier.None;
        var prototype = _proto.Index<EntityPrototype>(component.Prototype);
        if (prototype.TryGetComponent<ProjectileSpreadComponent>(out var ammoSpreadComp, _componentFactory))
        {
            shotCount = ammoSpreadComp.Count;
            shootModifier = ShootModifier.Spread;
        }

        _damageExamine.AddDamageExamineWithModifier(args.Message, damageSpec, shotCount, shootModifier, Loc.GetString("damage-projectile"));
    }

    private DamageSpecifier? GetProjectileDamage(string proto)
    {
        if (!ProtoManager.TryIndex<EntityPrototype>(proto, out var entityProto))
            return null;

        if (entityProto.Components
            .TryGetValue(_factory.GetComponentName(typeof(ProjectileComponent)), out var projectile))
        {
            var p = (ProjectileComponent) projectile.Component;

            if (!p.Damage.Empty)
            {
                return p.Damage;
            }
        }

        return null;
    }

    private void OnCartridgeExamine(EntityUid uid, CartridgeAmmoComponent component, ExaminedEvent args)
    {
        if (component.Spent)
        {
            args.PushMarkup(Loc.GetString("gun-cartridge-spent"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("gun-cartridge-unspent"));
        }
    }
}
