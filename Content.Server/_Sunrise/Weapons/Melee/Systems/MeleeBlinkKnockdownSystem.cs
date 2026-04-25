using Content.Server.Stunnable;
using Content.Shared._Sunrise.Weapons.Melee.Components;
using Content.Shared._Sunrise.Weapons.Melee.Events;
using Content.Shared.Maps;

namespace Content.Server._Sunrise.Weapons.Melee.Systems;

/// <summary>
/// Applies landing-tile knockdowns after a melee blink finishes.
/// </summary>
public sealed class MeleeBlinkKnockdownSystem : EntitySystem
{
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MeleeBlinkKnockdownComponent, MeleeBlinkLandedEvent>(OnBlinkLanded);
    }

    private void OnBlinkLanded(Entity<MeleeBlinkKnockdownComponent> ent, ref MeleeBlinkLandedEvent args)
    {
        foreach (var entity in _turf.GetEntitiesInTile(args.Coordinates, LookupFlags.Dynamic))
        {
            if (entity == args.User)
                continue;

            // Match blink-style impact handling by attempting a forced knockdown on entities occupying the landing tile.
            _stun.TryKnockdown(entity, ent.Comp.KnockdownDuration, force: true);
        }
    }
}
