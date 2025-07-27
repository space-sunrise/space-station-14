using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared._Sunrise.Felinid;
using Robust.Shared.Containers;

namespace Content.Server._Sunrise.Felinid;

/// <summary>
/// Система для возможности пиздиться фелинидами.
/// </summary>
public sealed class FelinidSystem : SharedFelinidSystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FelinidComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<FelinidContainerComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);
    }

    private void OnEntityRemoved(EntityUid uid,
        FelinidContainerComponent component,
        EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != BaseStorageId)
            return;

        if (!TryComp<FelinidComponent>(args.Entity, out var felinidComponent))
            return;

        felinidComponent.InContainer = false;
        Dirty(args.Entity, felinidComponent);
    }

    private void OnMeleeHit(EntityUid uid, FelinidComponent component, MeleeHitEvent args)
    {
        if (!args.IsHit ||
            !args.HitEntities.Any() ||
            args.User == uid)
        {
            return;
        }

        args.HitSoundOverride = component.DamageSound;
        args.BonusDamage = component.DamageBonus;
        _damageableSystem.TryChangeDamage(uid, component.FelinidDamage);
    }
}
