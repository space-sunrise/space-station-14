using System.Numerics;
using Content.Shared.Damage;
using Content.Shared._Sunrise.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._Sunrise.Weapons.Melee.Systems;

public abstract class SharedBackstabOnHitSystem : EntitySystem
{
    // Targets count as backstabbed when the attacker is anywhere in the rear hemisphere.
    private const float BackstabRearHemisphereDotThreshold = 0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BackstabOnHitComponent, GetMeleeHitBonusDamageEvent>(OnGetMeleeHitBonusDamage);
    }

    private void OnGetMeleeHitBonusDamage(Entity<BackstabOnHitComponent> ent, ref GetMeleeHitBonusDamageEvent args)
    {
        if (args.IsWideAttack)
            return;

        TryApplyBackstabBonus(ent, args.Target, args.User, ref args.BonusDamage);
    }

    public bool TryApplyBackstabBonus(Entity<BackstabOnHitComponent?> ent, EntityUid target, EntityUid user, ref DamageSpecifier bonusDamage)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!CanApplyBackstabBonus(target, user))
            return false;

        bonusDamage += ent.Comp.BonusDamage;
        return true;
    }

    public bool CanApplyBackstabBonus(EntityUid target, EntityUid user)
    {
        var targetTransform = Transform(target);
        var userTransform = Transform(user);
        var targetPosition = targetTransform.WorldPosition;
        var userPosition = userTransform.WorldPosition;
        var toUser = userPosition - targetPosition;
        var lengthSquared = toUser.LengthSquared();

        if (lengthSquared <= 0f)
            return false;

        var targetForward = targetTransform.WorldRotation.ToWorldVec();
        var targetToUser = toUser.Normalized();
        return Vector2.Dot(targetForward, targetToUser) <= BackstabRearHemisphereDotThreshold;
    }
}
