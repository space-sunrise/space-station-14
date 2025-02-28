using System.Linq;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared._Sunrise.Felinid;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server._Sunrise.Felinid;

/// <summary>
/// Система для возможности пиздиться фелинидами.
/// </summary>
public sealed class FelinidSystem : SharedFelinidSystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public const string BaseStorageId = "storagebase";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FelinidComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<FelinidContainerComponent, GetVerbsEvent<AlternativeVerb>>(AddInsertAltVerb);
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

    private void AddInsertAltVerb(EntityUid uid, FelinidContainerComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<FelinidComponent>(args.User, out var felinidComponent))
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                if (!_container.TryGetContainer(uid, BaseStorageId, out var storageContainer))
                    return;

                if (_containerSystem.Insert(args.User, storageContainer))
                {
                    felinidComponent.InContainer = true;
                    Dirty(args.User, felinidComponent);
                }
                else
                    _popupSystem.PopupEntity("Не удалось", args.User, args.User);
            },
            Text = "Залезть внутрь",
            Priority = 2
        };
        args.Verbs.Add(verb);
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
