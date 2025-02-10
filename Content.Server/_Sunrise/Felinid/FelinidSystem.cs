using System.Linq;
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
public sealed class FelinidSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public const string BaseStorageId = "storagebase";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FelinidComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<FelinidContainerComponent, GetVerbsEvent<AlternativeVerb>>(AddInsertAltVerb);
    }

    private void AddInsertAltVerb(EntityUid uid, FelinidContainerComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!HasComp<FelinidComponent>(args.User))
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                if (!_container.TryGetContainer(uid, BaseStorageId, out var storageContainer))
                    return;

                _containerSystem.Insert(args.User, storageContainer);
            },
            Text = "Залезть внутрь",
            Priority = 2
        };
        args.Verbs.Add(verb);
    }

    private void OnMeleeHit(EntityUid uid, FelinidComponent component, MeleeHitEvent args)
    {
        if (!args.IsHit ||
            !args.HitEntities.Any())
        {
            return;
        }

        args.BonusDamage = component.DamageBonus;
        _damageableSystem.TryChangeDamage(uid, component.FelinidDamage);
    }
}
