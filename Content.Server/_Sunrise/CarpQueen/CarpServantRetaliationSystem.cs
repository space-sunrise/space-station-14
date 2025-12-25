using Content.Shared._Sunrise.CarpQueen;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;

namespace Content.Server._Sunrise.CarpQueen;

/// <summary>
/// System that makes tamed carps retaliate against entities that damage their remembered friends.
/// </summary>
public sealed class CarpServantRetaliationSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(EntityUid uid, DamageableComponent component, DamageChangedEvent args)
    {
        // Only react to damage increases
        if (!args.DamageIncreased)
            return;

        // Get the damaged entity (uid is the entity that received damage)
        var damagedEntity = uid;

        // Get the attacker
        if (args.Origin is not { } attacker)
            return;

        // Don't retaliate against inanimate objects
        if (!HasComp<MobStateComponent>(attacker))
            return;

        // Find all carps that remember the damaged entity as a friend
        var query = EntityQueryEnumerator<CarpServantMemoryComponent>();
        while (query.MoveNext(out var carpUid, out var memory))
        {
            // Skip if carp is being deleted
            if (TerminatingOrDeleted(carpUid))
                continue;

            // Check if the damaged entity is one of this carp's remembered friends
            if (!memory.RememberedFriends.Contains(damagedEntity))
                continue;

            var attackerIsFriend = memory.RememberedFriends.Contains(attacker);
            var isServant = TryComp<CarpQueenServantComponent>(carpUid, out var servant) && servant.Queen != null;

            if (isServant)
            {
                // Servants only retaliate when the queen is harmed
                if (servant!.Queen != damagedEntity)
                    continue;
            }
            else
            {
                // Free-roaming carp do not retaliate against other remembered friends
                if (attackerIsFriend)
                    continue;
            }

            // Aggro on the attacker
            var exception = EnsureComp<FactionExceptionComponent>(carpUid);

            // Also handle discipline logic: if attacker was in forbidden targets, remove it
            // This allows the carp to attack again after the attacker damaged the owner
            if (memory.ForbiddenTargets.Remove(attacker))
            {
                Dirty(carpUid, memory);
            }

            // Unignore and aggro the attacker (allows attack after discipline was cleared)
            _npcFaction.UnignoreEntity((carpUid, exception), attacker);
            _npcFaction.AggroEntity((carpUid, exception), (attacker, null));
        }
    }
}

