using Content.Shared._Sunrise.Particles;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanBasicDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicDamageComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
        // Sunrise added start - support projectile components in hitscans
        SubscribeLocalEvent<ProjectileComponent, HitscanRaycastFiredEvent>(OnProjectileHitscanHit);
        // Sunrise added end
    }

    private void OnHitscanHit(Entity<HitscanBasicDamageComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        var dmg = ent.Comp.Damage * _damage.UniversalHitscanDamageModifier;

        // Sunrise edit start - credit pilot player as origin for damage overlay compatibility
        var origin = args.Data.Shooter ?? args.Data.Gun;
        RaiseParticleImpact(args.Data.HitEntity.Value, dmg, origin, args.Data);
        if(!_damage.TryChangeDamage(args.Data.HitEntity.Value, dmg, out var damageDealt, origin: origin))
            return;
        // Sunrise edit end

        var damageEvent = new HitscanDamageDealtEvent
        {
            Target = args.Data.HitEntity.Value,
            DamageDealt = damageDealt,
        };

        RaiseLocalEvent(ent, ref damageEvent);
    }

    // Sunrise added start - support projectile components in hitscans
    private void OnProjectileHitscanHit(Entity<ProjectileComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (HasComp<HitscanBasicDamageComponent>(ent))
            return;

        if (args.Data.HitEntity == null)
            return;

        var dmg = ent.Comp.Damage * _damage.UniversalHitscanDamageModifier;

        // Sunrise edit start - credit pilot player as origin for damage overlay compatibility
        var origin = args.Data.Shooter ?? args.Data.Gun;
        RaiseParticleImpact(args.Data.HitEntity.Value, dmg, origin, args.Data);
        if (!_damage.TryChangeDamage(args.Data.HitEntity.Value, dmg, out var damageDealt, origin: origin))
            return;
        // Sunrise edit end

        var damageEvent = new HitscanDamageDealtEvent
        {
            Target = args.Data.HitEntity.Value,
            DamageDealt = damageDealt,
        };

        RaiseLocalEvent(ent, ref damageEvent);
    }
    // Sunrise added end

    // Sunrise added start - единая передача точной raycast-позиции материал-зависимым частицам.
    private void RaiseParticleImpact(
        EntityUid target,
        DamageSpecifier damage,
        EntityUid origin,
        HitscanRaycastFiredData data)
    {
        if (data.HitPosition is not { } hitPosition)
            return;

        var particleImpact = new ParticleDamageImpactEvent(
            damage,
            origin,
            hitPosition,
            data.ShotDirection);
        RaiseLocalEvent(target, ref particleImpact);
    }
    // Sunrise added end
}
