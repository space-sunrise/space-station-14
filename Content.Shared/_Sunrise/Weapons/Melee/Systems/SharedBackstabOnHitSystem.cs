using System.Numerics;
using Content.Shared.Damage;
using Content.Shared._Sunrise.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._Sunrise.Weapons.Melee.Systems;

public abstract class SharedBackstabOnHitSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // Targets count as backstabbed when the attacker is anywhere in the rear hemisphere.
    private const float BackstabRearHemisphereDotThreshold = 0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BackstabOnHitComponent, GetMeleeHitBonusDamageEvent>(OnGetMeleeHitBonusDamage);
    }

    protected virtual void OnGetMeleeHitBonusDamage(Entity<BackstabOnHitComponent> ent, ref GetMeleeHitBonusDamageEvent args)
    {
        if (args.IsWideAttack)
            return;

        if (!TryApplyBackstabBonus(ent.AsNullable(), args.Target, args.User, ref args.BonusDamage))
            return;

        OnBackstabBonusApplied(ent, args.Target);
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
        var targetPosition = _transform.GetWorldPosition(targetTransform);
        var userPosition = _transform.GetWorldPosition(userTransform);
        var toUser = userPosition - targetPosition;
        var lengthSquared = toUser.LengthSquared();

        if (lengthSquared <= 0f)
            return false;

        var targetForward = _transform.GetWorldRotation(targetTransform).ToWorldVec();
        var targetToUser = toUser / MathF.Sqrt(lengthSquared);
        return Vector2.Dot(targetForward, targetToUser) <= BackstabRearHemisphereDotThreshold;
    }

    protected void ApplyBackstabBonus(Entity<BackstabOnHitComponent> ent, ref DamageSpecifier bonusDamage)
    {
        bonusDamage += ent.Comp.BonusDamage;
    }

    protected virtual void OnBackstabBonusApplied(Entity<BackstabOnHitComponent> ent, EntityUid target)
    {
    }
}
