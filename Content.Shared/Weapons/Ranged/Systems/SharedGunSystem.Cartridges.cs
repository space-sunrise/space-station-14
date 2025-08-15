using Content.Shared._Starlight.Weapon.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    [Dependency] private readonly DamageExamineSystem _damageExamine = default!;

    // needed for server system
    protected virtual void InitializeCartridge()
    {
        SubscribeLocalEvent<CartridgeAmmoComponent, ExaminedEvent>(OnCartridgeExamine);
        SubscribeLocalEvent<CartridgeAmmoComponent, DamageExamineEvent>(OnCartridgeDamageExamine);

        SubscribeLocalEvent<HitScanCartridgeAmmoComponent, ExaminedEvent>(OnHitScanCartridgeExamine);
        SubscribeLocalEvent<HitScanCartridgeAmmoComponent, DamageExamineEvent>(OnHitScanCartridgeDamageExamine);
    }

    private void OnCartridgeExamine(Entity<CartridgeAmmoComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(ent.Comp.Spent
            ? Loc.GetString("gun-cartridge-spent")
            : Loc.GetString("gun-cartridge-unspent"));
    }

    private void OnHitScanCartridgeExamine(Entity<HitScanCartridgeAmmoComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(ent.Comp.Spent
            ? Loc.GetString("gun-cartridge-spent")
            : Loc.GetString("gun-cartridge-unspent"));
    }

    private void OnCartridgeDamageExamine(EntityUid uid, CartridgeAmmoComponent component, ref DamageExamineEvent args)
    {
        var damageSpec = GetProjectileDamage(component.Prototype);

        if (damageSpec == null)
            return;

        _damageExamine.AddDamageExamine(args.Message, Damageable.ApplyUniversalAllModifiers(damageSpec), Loc.GetString("damage-projectile"));
    }

    private DamageSpecifier? GetProjectileDamage(string proto)
    {
        if (!ProtoManager.TryIndex<EntityPrototype>(proto, out var entityProto))
            return null;

        if (entityProto.Components
            .TryGetValue(Factory.GetComponentName<ProjectileComponent>(), out var projectile))
        {
            var p = (ProjectileComponent) projectile.Component;

            if (!p.Damage.Empty)
            {
                return p.Damage * Damageable.UniversalProjectileDamageModifier;
            }
        }

        return null;
    }

    private void OnHitScanCartridgeDamageExamine(EntityUid uid, HitScanCartridgeAmmoComponent component, ref DamageExamineEvent args) {
        var damageSpec = GetHitscanProjectileDamage(component.Hitscan);
        if (damageSpec == null)
            return;

        _damageExamine.AddDamageExamine(args.Message, Damageable.ApplyUniversalAllModifiers(damageSpec), Loc.GetString("damage-projectile"));

        var ArmorMessage = GetArmorPenetrationExplain(component.Hitscan);

        args.Message.AddMessage(ArmorMessage);

    }

    private FormattedMessage GetArmorPenetrationExplain(string proto) {
        var msg = new FormattedMessage();
        if (!ProtoManager.TryIndex<HitscanPrototype>(proto,out var entityProto))
            return msg;

        if (entityProto.ArmorPenetration == 0) {
            return msg;
        }
        if (entityProto.ArmorPenetration > 0){
            msg.PushNewline();
            msg.TryAddMarkup(Loc.GetString("damage-examine-penetration-positive",("penetration", MathF.Round(entityProto.ArmorPenetration * 100, 1))), out var error);
        }
        if(entityProto.ArmorPenetration < 0) {
            msg.PushNewline();
            msg.TryAddMarkup(Loc.GetString("damage-examine-penetration-negative",("penetration", MathF.Round(entityProto.ArmorPenetration * -100, 1))), out var error);
        }
        return msg;
    }

    private DamageSpecifier? GetHitscanProjectileDamage(string proto) {
        if (!ProtoManager.TryIndex<HitscanPrototype>(proto,out var entityProto))
            return null;

        if (entityProto.Damage == null) {
            return null;
        }

        if (!entityProto.Damage.Empty) {
            return entityProto.Damage * Damageable.UniversalHitscanDamageModifier;
        }

        return null;
    }
}
