using Content.Shared.Damage.Systems;
using Content.Shared.Database; // Sunrise-edit
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Administration.Logs; // Sunrise-edit

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanBasicDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly ISharedAdminLogManager _log = default!; // Sunrise-edit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicDamageComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanBasicDamageComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        var dmg = ent.Comp.Damage * _damage.UniversalHitscanDamageModifier;

        // var damageDealt = _damage.TryChangeDamage(args.Data.HitEntity.Value, dmg, origin: args.Data.Gun); // Starlight - we redefine this
        // Starlight start
        var damageDealt = _damage.ChangeDamage(
                args.Data.HitEntity.Value,
                dmg,
                ignoreResistances: ent.Comp.IgnoreResistances,
                origin: args.Data.Gun,
                armorPenetration: ent.Comp.ArmorPenetration,
                canHeal: false
            );
        // Starlight end

        if (damageDealt == null)
            return;

        // Sunrise-start
        _log.Add(
            LogType.Damaged,
            $"{ToPrettyString(args.Data.Shooter):user} damaged {ToPrettyString(args.Data.HitEntity):target}"
                + $" using {ToPrettyString(args.Data.Gun):entity} by {damageDealt.GetTotal():0.##}."
        );
        // Sunrise-end

        var damageEvent = new HitscanDamageDealtEvent
        {
            Target = args.Data.HitEntity.Value,
            DamageDealt = damageDealt,
            Data = args.Data, // Starlight
        };

        RaiseLocalEvent(ent, ref damageEvent);
    }
}
