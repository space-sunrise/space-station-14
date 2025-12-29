using System.Numerics;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Popups;
using Content.Server.Chat.Systems;
using Content.Shared._Sunrise.CarpQueen;
using Content.Shared.Pointing;
using Content.Shared.Random.Helpers;
using Content.Shared.Dataset;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.RatKing;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Damage;

namespace Content.Server._Sunrise.CarpQueen;

public sealed class CarpQueenSystem : SharedCarpQueenSystem
{
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly CarpEggSystem _carpEggs = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CarpQueenComponent, CarpQueenSummonActionEvent>(OnSummon);
        SubscribeLocalEvent<CarpQueenComponent, AfterPointedAtEvent>(OnPointedAt);
        SubscribeLocalEvent<CarpQueenServantComponent, ComponentShutdown>(OnServantShutdown);
    }

    protected override void OnStartup(EntityUid uid, CarpQueenComponent component, ComponentStartup args)
    {
        base.OnStartup(uid, component, args);

        if (TryComp<HungerComponent>(uid, out var hunger))
            component.LastObservedHunger = _hunger.GetHunger(hunger);
    }

    private void OnSummon(EntityUid uid, CarpQueenComponent component, CarpQueenSummonActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.ArmyMobSpawnOptions.Count == 0)
            return;

        // Limit total eggs + servants to MaxArmySize. Prune invalid references first.
        var toRemoveServants = new List<EntityUid>();
        var aliveServants = 0;
        foreach (var s in component.Servants)
        {
            if (!Exists(s))
            {
                toRemoveServants.Add(s);
                continue;
            }
            if (TryComp<MobStateComponent>(s, out var mobState) && mobState.CurrentState == MobState.Dead)
                continue;
            aliveServants++;
        }
        foreach (var rem in toRemoveServants)
            component.Servants.Remove(rem);

        var toRemoveEggs = new List<EntityUid>();
        foreach (var e in component.Eggs)
        {
            if (!Exists(e))
                toRemoveEggs.Add(e);
        }
        foreach (var rem in toRemoveEggs)
            component.Eggs.Remove(rem);

        var eggsCount = component.Eggs.Count;
        if (aliveServants + eggsCount >= component.MaxArmySize)
        {
            _popup.PopupEntity(Loc.GetString("carp-queen-max-army", ("amount", component.MaxArmySize)), uid, uid);
            return;
        }

        // Hunger cost like Rat King
        if (!TryComp<HungerComponent>(uid, out var hungerComp))
            return;

        if (_hunger.GetHunger(hungerComp) < component.HungerPerSummon)
        {
            _popup.PopupEntity(Loc.GetString("rat-king-too-hungry"), uid, uid);
            return;
        }

        args.Handled = true;
        _hunger.ModifyHunger(uid, -component.HungerPerSummon, hungerComp);
        // Spawn egg instead of immediate servant
        var egg = Spawn("MobCarpEgg", Transform(uid).Coordinates);
        var eggComp = EnsureComp<CarpEggComponent>(egg);
        eggComp.Queen = uid;
        Dirty(egg, eggComp);
        component.Eggs.Add(egg);

        // Trigger hatch check now that queen is assigned (covers tiles already containing liquid/FloorWater)
        _carpEggs.RequestHatchCheck(egg);
        _popup.PopupEntity(Loc.GetString("carp-queen-summon-popup"), uid, uid);
    }

    private void OnServantShutdown(EntityUid uid, CarpQueenServantComponent servant, ComponentShutdown args)
    {
        if (servant.Queen == null || !TryComp(servant.Queen.Value, out CarpQueenComponent? queen))
            return;

        queen.Servants.Remove(uid);
    }

    private void OnPointedAt(EntityUid uid, CarpQueenComponent component, ref AfterPointedAtEvent args)
    {
        if (component.CurrentOrder != CarpQueenOrderType.Kill)
            return;

        var target = args.Pointed;
        if (!Exists(target))
            return;

        // Accept any living mob (players or AI). Ignore objects.
        var valid = false;
        if (TryComp<MobStateComponent>(target, out var mobState))
            valid = mobState.CurrentState != MobState.Dead;
        else if (HasComp<NpcFactionMemberComponent>(target))
            valid = true;
        else if (HasComp<ActorComponent>(target))
            valid = true;

        if (!valid)
            return;

        foreach (var servant in component.Servants)
        {
            // Skip if servant is being deleted or doesn't exist
            if (TerminatingOrDeleted(servant))
                continue;

            if (TryComp<CarpServantMemoryComponent>(servant, out var memory))
            {
                var exception = EnsureComp<FactionExceptionComponent>(servant);

                if (_npcFaction.IsIgnored((servant, exception), target))
                    _npcFaction.UnignoreEntity((servant, exception), target);

                if (memory.RememberedFriends.Remove(target))
                    Dirty(servant, memory);

                if (memory.ForbiddenTargets.Remove(target))
                    Dirty(servant, memory);
            }

            _npc.SetBlackboard(servant, NPCBlackboard.CurrentOrderedTarget, target);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Small self-heal when hunger increases (i.e., when eating).
        var query = EntityQueryEnumerator<CarpQueenComponent, HungerComponent>();
        while (query.MoveNext(out var uid, out var queen, out var hunger))
        {
            var current = _hunger.GetHunger(hunger);
            if (current > queen.LastObservedHunger)
            {
                var delta = current - queen.LastObservedHunger;
                var heal = MathF.Min(delta * queen.HealPerHunger, queen.MaxHealPerTick);
                if (heal > 0f)
                {
                    var spec = new DamageSpecifier();
                    spec.DamageDict["Blunt"] = -heal / 2f;
                    spec.DamageDict["Slash"] = -heal / 2f;
                    spec.DamageDict["Heat"] = 0f; // leave as 0; can be adjusted later
                    _damageable.TryChangeDamage(uid, spec, true, false);
                }
            }

            queen.LastObservedHunger = current;
        }
    }

    public override void UpdateServantNpc(EntityUid uid, CarpQueenOrderType orderType)
    {
        base.UpdateServantNpc(uid, orderType);

        // Only update if this is actually a servant (has CarpQueenServantComponent)
        if (!TryComp<CarpQueenServantComponent>(uid, out var servant) || servant.Queen == null || !Exists(servant.Queen.Value))
            return;

        if (!TryComp<HTNComponent>(uid, out var htn))
            return;

        if (htn.Plan != null)
            _htn.ShutdownPlan(htn);

        // Servant always follows queen and executes her commands
        _npc.SetBlackboard(uid, NPCBlackboard.FollowTarget, new EntityCoordinates(servant.Queen.Value, Vector2.Zero));

        // Configure order and follow distances as requested (close follow ~1 tile)
        // Convert CarpQueenOrderType to RatKingOrderType for HTN compatibility
        var ratKingOrder = SharedCarpQueenSystem.ConvertToRatKingOrder(orderType);
        _npc.SetBlackboard(uid, NPCBlackboard.CurrentOrders, ratKingOrder);
        _npc.SetBlackboard(uid, "FollowCloseRange", 1.0f);
        _npc.SetBlackboard(uid, "FollowRange", 1.5f);
        _htn.Replan(htn);
    }


    public override void DoCommandCallout(EntityUid uid, CarpQueenComponent component)
    {
        base.DoCommandCallout(uid, component);

        if (!component.OrderCallouts.TryGetValue(component.CurrentOrder, out var datasetId) ||
            !PrototypeManager.TryIndex<LocalizedDatasetPrototype>(datasetId, out var datasetPrototype))
            return;

        var msg = Random.Pick(datasetPrototype);
        _chat.TrySendInGameICMessage(uid, msg, InGameICChatType.Speak, true);
    }
}


