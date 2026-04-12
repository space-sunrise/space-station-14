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

        TryApplyBackstabBonus(ent.AsNullable(), args.Target, args.User, ref args.BonusDamage);
    }

    protected bool TryApplyBackstabBonus(Entity<BackstabOnHitComponent?> ent, EntityUid target, EntityUid user, ref DamageSpecifier bonusDamage)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!CanApplyBackstabBonus(target, user))
            return false;

        ApplyBackstabBonus((ent.Owner, ent.Comp!), ref bonusDamage);
        return true;
    }

    /// <summary>
    /// Checks whether the attacker is inside the target's rear hemisphere and therefore qualifies for a backstab bonus.
    /// </summary>
    protected bool CanApplyBackstabBonus(EntityUid target, EntityUid user)
    {
        var targetTransform = Transform(target);
        var userTransform = Transform(user);
        var targetPosition = TransformSystem.GetWorldPosition(targetTransform);
        var userPosition = TransformSystem.GetWorldPosition(userTransform);
        var toUser = userPosition - targetPosition;
        var lengthSquared = toUser.LengthSquared();

        if (lengthSquared <= 0f)
            return false;

        var targetForward = TransformSystem.GetWorldRotation(targetTransform).ToWorldVec();
        var targetToUser = toUser / MathF.Sqrt(lengthSquared);
        return Vector2.Dot(targetForward, targetToUser) <= BackstabRearHemisphereDotThreshold;
    }

    protected void ApplyBackstabBonus(Entity<BackstabOnHitComponent> ent, ref DamageSpecifier bonusDamage)
    {
        bonusDamage += ent.Comp.BonusDamage;
    }
}
