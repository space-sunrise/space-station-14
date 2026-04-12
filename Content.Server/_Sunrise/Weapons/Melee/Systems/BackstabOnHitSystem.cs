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

        if (!TryApplyBackstabBonus(ent, args.Target, args.User, ref args.BonusDamage))
            return;

        if (ent.Comp.PopupMessages.Count == 0)
            return;

        _popup.PopupEntity(Loc.GetString(PickPopup(ent.Comp)), args.Target);
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
        var targetPosition = _transform.GetWorldPosition(target);
        var userPosition = _transform.GetWorldPosition(user);
        var toUser = userPosition - targetPosition;

        if (toUser.LengthSquared() <= 0f)
            return false;

        var targetForward = _transform.GetWorldRotation(target).ToWorldVec();
        var targetToUser = toUser.Normalized();

        return Vector2.Dot(targetForward, targetToUser) <= BackstabRearHemisphereDotThreshold;
    }

    private LocId PickPopup(BackstabOnHitComponent component)
    {
        if (component.PopupMessages.Count == 0)
            return string.Empty;

        if (component.PopupWeights.Count != component.PopupMessages.Count || component.PopupWeights.Count == 0)
            return _random.Pick(component.PopupMessages);

        var totalWeight = 0f;
        foreach (var weight in component.PopupWeights)
        {
            if (weight > 0f)
                totalWeight += weight;
        }

        if (totalWeight <= 0f)
            return _random.Pick(component.PopupMessages);

        var roll = _random.NextFloat() * totalWeight;
        for (var i = 0; i < component.PopupMessages.Count; i++)
        {
            var weight = component.PopupWeights[i];
            if (weight <= 0f)
                continue;

            if (roll < weight)
                return component.PopupMessages[i];

            roll -= weight;
        }

        return component.PopupMessages[^1];
    }
}
