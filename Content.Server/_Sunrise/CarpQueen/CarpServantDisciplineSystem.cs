using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._Sunrise.CarpQueen;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Inventory;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._Sunrise.CarpQueen;

/// <summary>
/// System that handles discipline for tamed carps (hand-raised carps).
/// If the owner hits their tamed carp with bare hands or gloves, the carp stops attacking its current target.
/// The carp will resume attacking that target if the attacker damages the owner.
/// </summary>
public sealed class CarpServantDisciplineSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CarpServantMemoryComponent, AttackedEvent>(OnCarpAttacked);
        // Note: We don't subscribe to DamageChangedEvent here to avoid duplicate with CarpServantRetaliationSystem
        // The discipline logic (removing from forbidden targets) is handled separately
    }

    private void OnCarpAttacked(EntityUid uid, CarpServantMemoryComponent memory, AttackedEvent args)
    {
        // Only handle if this is a tamed carp (not a queen's servant)
        if (HasComp<CarpQueenServantComponent>(uid))
            return;

        // Check if attacker is one of the remembered friends (owner)
        if (!memory.RememberedFriends.Contains(args.User))
            return;

        // Check if attack was made with bare hands or something in gloves slot
        // Used == User means no weapon (bare hands)
        // Or Used is an item equipped in the gloves slot of the user
        var isBareHands = args.Used == args.User;
        var isGlovesSlotItem = _inventory.TryGetSlotEntity(args.User, "gloves", out var glovesSlotItem) && glovesSlotItem == args.Used;

        if (!isBareHands && !isGlovesSlotItem)
            return;

        // Get current attack target from NPCMeleeCombatComponent or blackboard
        EntityUid? currentTarget = null;
        NPCMeleeCombatComponent? meleeCombat = null;
        HTNComponent? htn = null;

        // Try to get from NPCMeleeCombatComponent first (most direct)
        if (TryComp<NPCMeleeCombatComponent>(uid, out meleeCombat) && meleeCombat.Target != EntityUid.Invalid)
        {
            currentTarget = meleeCombat.Target;
        }
        // Fallback to blackboard - check common target keys
        else if (TryComp<HTNComponent>(uid, out htn))
        {
            // Try CurrentOrderedTarget first (set by queen/orders)
            if (htn.Blackboard.TryGetValue<EntityUid>(NPCBlackboard.CurrentOrderedTarget, out var orderedTarget, EntityManager) && orderedTarget != EntityUid.Invalid)
                currentTarget = orderedTarget;
            // Then try generic "Target" key
            else if (htn.Blackboard.TryGetValue<EntityUid>("Target", out var target, EntityManager) && target != EntityUid.Invalid)
                currentTarget = target;
        }

        // If there's a current target, add it to forbidden targets
        if (currentTarget != null && currentTarget != args.User)
        {
            memory.ForbiddenTargets.Add(currentTarget.Value);
            Dirty(uid, memory);

            // Add to faction exceptions to prevent attack
            var exception = EnsureComp<FactionExceptionComponent>(uid);
            if (!_npcFaction.IsIgnored((uid, exception), currentTarget.Value))
            {
                _npcFaction.IgnoreEntity((uid, exception), (currentTarget.Value, null));
            }

            // Clear the attack target from blackboard/combat component
            if (meleeCombat != null)
            {
                meleeCombat.Target = EntityUid.Invalid;
            }
            else if (htn != null)
            {
                // Clear target keys from blackboard
                if (htn.Blackboard.ContainsKey("Target"))
                    _npc.SetBlackboard(uid, "Target", EntityUid.Invalid);
                if (htn.Blackboard.ContainsKey(NPCBlackboard.CurrentOrderedTarget))
                    _npc.SetBlackboard(uid, NPCBlackboard.CurrentOrderedTarget, EntityUid.Invalid);
            }
        }
    }
}

