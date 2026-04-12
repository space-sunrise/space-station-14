using System.Numerics;
using Content.Server._Sunrise.Weapons.Melee.Components;
using Content.Server.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Weapons.Melee.Systems;

public sealed class BackstabOnHitSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float BackstabDotProductThreshold = 0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BackstabOnHitComponent, GetMeleeHitBonusDamageEvent>(OnGetMeleeHitBonusDamage);
    }

    private void OnGetMeleeHitBonusDamage(Entity<BackstabOnHitComponent> ent, ref GetMeleeHitBonusDamageEvent args)
    {
        if (!TryApplyBackstabBonus(ent, args.Target, args.User, ref args.BonusDamage))
            return;

        if (ent.Comp.PopupMessages.Count == 0)
            return;

        _popup.PopupEntity(Loc.GetString(_random.Pick(ent.Comp.PopupMessages)), args.Target);
    }

    public bool TryApplyBackstabBonus(Entity<BackstabOnHitComponent?> ent, EntityUid target, EntityUid user, ref DamageSpecifier bonusDamage)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!CanApplyBackstabBonus(target, user))
            return false;

        bonusDamage += ent.Comp.Damage;
        return true;
    }

    public bool CanApplyBackstabBonus(EntityUid target, EntityUid user)
    {
        var targetPosition = _transform.GetWorldPosition(target);
        var userPosition = _transform.GetWorldPosition(user);
        var toUser = userPosition - targetPosition;

        if (toUser.LengthSquared() <= 0f)
            return false;

        var targetForward = _transform.GetWorldRotation(target).ToWorldVec();
        var targetToUser = toUser.Normalized();

        return Vector2.Dot(targetForward, targetToUser) <= BackstabDotProductThreshold;
    }
}
