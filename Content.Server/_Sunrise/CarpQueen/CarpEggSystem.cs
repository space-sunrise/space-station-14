using System.Numerics;
using Content.Server.Fluids.EntitySystems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Popups;
using Content.Shared._Sunrise.CarpQueen;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.RatKing;
using Content.Shared.Humanoid;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Content.Shared.Maps;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Maths;

namespace Content.Server._Sunrise.CarpQueen;

public sealed class CarpEggSystem : CarpQueenAccessSystem
{
    [Dependency] private readonly PuddleSystem _puddles = default!;
    [Dependency] private readonly IPrototypeManager _protos = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _xformSys = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly CarpQueenSystem _carpQueenSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CarpEggComponent, DestructionEventArgs>(OnEggDestroyed);
        SubscribeLocalEvent<CarpEggComponent, ComponentShutdown>(OnEggShutdown);
        SubscribeLocalEvent<CarpEggComponent, MapInitEvent>(OnEggMapInit);
        SubscribeLocalEvent<CarpEggComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<CarpEggComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<SolutionChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<CarpQueenServantComponent, ComponentStartup>(OnServantStartup);
        SubscribeLocalEvent<PuddleComponent, MapInitEvent>(OnPuddleMapInit);
        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CarpEggComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var egg, out var xform))
        {
            // If not currently eligible, periodically re-check conditions to become eligible
            if (!egg.Eligible)
            {
                egg.Accum += frameTime;
                egg.WaitElapsed += frameTime;
                if (egg.Accum >= egg.CheckInterval)
                {
                    egg.Accum = 0f;
                    TryHatchCheck(uid, egg);
                }

                // If waited too long without liquid, destroy the egg (same as if it was broken)
                if (egg.WaitElapsed >= egg.MaxWaitWithoutLiquid)
                {
                    _destructible.DestroyEntity(uid);
                    continue;
                }
                continue;
            }

            // Eligible: count down to hatch
            egg.Accum += frameTime;
            if (egg.Accum >= egg.HatchDelay)
            {
                // Validate still on liquid before hatching
                if (TryComp<MapGridComponent>(xform.GridUid, out var grid))
                {
                    var tile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
                    if (HasSufficientLiquid(tile, egg.RequiredVolume))
                    {
                        Hatch(uid, egg, xform);
                        continue;
                    }
                }

                // Conditions no longer valid
                egg.Eligible = false;
                egg.Accum = 0f;
                // Do not reset WaitElapsed here so total wait continues accumulating until liquid appears
                ResetVisual(uid);
            }
        }
    }

    private void OnServantStartup(EntityUid uid, CarpQueenServantComponent servant, ComponentStartup args)
    {
        // Queen present: follow and use her current orders
        if (servant.Queen != null && TryComp(servant.Queen.Value, out CarpQueenComponent? queen))
        {
            _npc.SetBlackboard(uid, NPCBlackboard.FollowTarget, new EntityCoordinates(servant.Queen.Value, Vector2.Zero));
            // Convert CarpQueenOrderType to RatKingOrderType for HTN compatibility
            var ratKingOrder = SharedCarpQueenSystem.ConvertToRatKingOrder(queen.CurrentOrder);
            _npc.SetBlackboard(uid, NPCBlackboard.CurrentOrders, ratKingOrder);
            _npc.SetBlackboard(uid, "FollowCloseRange", 1.0f);
            _npc.SetBlackboard(uid, "FollowRange", 1.5f);
        }
        else
        {
            // No queen: default to Loose so directly spawned servants are active
            // Convert to RatKingOrderType for HTN compatibility
            _npc.SetBlackboard(uid, NPCBlackboard.CurrentOrders, RatKingOrderType.Loose);
        }

        // If HTN is already present, force a replan now
        if (TryComp<HTNComponent>(uid, out var htn))
        {
            if (htn.Plan != null)
                _htn.ShutdownPlan(htn);
            _htn.Replan(htn);
        }
    }

    private void OnEggMapInit(EntityUid uid, CarpEggComponent egg, MapInitEvent args)
    {
        // Defer until queen is assigned to avoid spawning unlinked servants
        if (egg.Queen == null)
            return;

        TryHatchCheck(uid, egg);
    }

    private void OnAnchorChanged(EntityUid uid, CarpEggComponent egg, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            TryHatchCheck(uid, egg);
    }

    private void OnRemovedFromContainer(EntityUid uid, CarpEggComponent egg, EntGotRemovedFromContainerMessage args)
    {
        TryHatchCheck(uid, egg);
    }

    private void OnSolutionChanged(ref SolutionChangedEvent args)
    {
        // If a puddle changed, re-check eggs on that tile
        // Skip if entity is being deleted or doesn't have required components
        if (!TryComp<PuddleComponent>(args.Solution.Owner, out var _))
            return;

        // Additional safety check - ensure entity is valid
        if (TerminatingOrDeleted(args.Solution.Owner))
            return;

        var xform = Transform(args.Solution.Owner);
        if (xform.GridUid == null)
            return;
        if (!TryComp<MapGridComponent>(xform.GridUid.Value, out var grid))
            return;
        var tile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
        foreach (var ent in _lookup.GetEntitiesInTile(tile))
        {
            if (TryComp<CarpEggComponent>(ent, out var egg))
                TryHatchCheck(ent, egg);
        }
    }

    private void OnPuddleMapInit(EntityUid uid, PuddleComponent puddle, MapInitEvent args)
    {
        // New puddle spawned: check eggs on this tile
        var xform = Transform(uid);
        if (xform.GridUid == null)
            return;
        if (!TryComp<MapGridComponent>(xform.GridUid.Value, out var grid))
            return;
        var tile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
        foreach (var ent in _lookup.GetEntitiesInTile(tile))
        {
            if (TryComp<CarpEggComponent>(ent, out var egg))
                TryHatchCheck(ent, egg);
        }
    }

    private void OnTileChanged(ref TileChangedEvent ev)
    {
        if (!TryComp<MapGridComponent>(ev.Entity, out var grid))
            return;

        foreach (var change in ev.Changes)
        {
            var tile = _map.GetTileRef(ev.Entity, grid, change.GridIndices);
            foreach (var ent in _lookup.GetEntitiesInTile(tile))
            {
                if (TryComp<CarpEggComponent>(ent, out var egg))
                    TryHatchCheck(ent, egg);
            }
        }
    }



    private void TryHatchCheck(EntityUid uid, CarpEggComponent egg)
    {
        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        // Only hatch if not inside containers
        if (xform.GridUid == null)
            return;
        if (_containers.IsEntityInContainer(uid))
            return;

        if (!TryComp<MapGridComponent>(xform.GridUid.Value, out var grid))
            return;
        var tile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);

        if (HasSufficientLiquid(tile, egg.RequiredVolume))
        {
            if (!egg.Eligible)
            {
                egg.Eligible = true;
                egg.Accum = 0f;
                egg.WaitElapsed = 0f;
                // Show popup locally to queen if present, otherwise to all nearby
                if (egg.Queen != null && Exists(egg.Queen.Value))
                    _popup.PopupEntity(Loc.GetString("carp-egg-activates"), uid, egg.Queen.Value);
                else
                    _popup.PopupEntity(Loc.GetString("carp-egg-activates"), uid);
            }
            UpdateVisualForTile(uid, tile);
        }
        else
        {
            if (egg.Eligible)
            {
                egg.Eligible = false;
                egg.Accum = 0f;
                ResetVisual(uid);
            }
        }
    }

    private bool HasSufficientLiquid(TileRef tile, float required)
    {
        // Puddle volume check
        if (_puddles.TryGetPuddle(tile, out var puddle))
        {
            var vol = _puddles.CurrentVolume(puddle);
            if (vol >= FixedPoint2.New(required))
                return true;
        }

        // Floor water entity check counts as sufficient
        var gridId = tile.GridUid;
        if (gridId != null)
        {
            // Check anchored entities first
            if (gridId is { } gid && TryComp<MapGridComponent>(gid, out var grid))
            {
                var enumerator = _map.GetAnchoredEntitiesEnumerator(gid, grid, tile.GridIndices);
                while (enumerator.MoveNext(out EntityUid? ent))
                {
                    if (!ent.HasValue)
                        continue;

                    // Check by prototype ID
                    var meta = MetaData(ent.Value);
                    if (meta.EntityPrototype?.ID == "FloorWaterEntity")
                        return true;
                }
            }

            // Also check all entities in tile (fallback)
            var entities = _lookup.GetEntitiesInTile(tile);
            foreach (var ent in entities)
            {
                var meta = MetaData(ent);
                if (meta.EntityPrototype?.ID == "FloorWaterEntity")
                    return true;
            }
        }

        return false;
    }

    private void UpdateVisualForTile(EntityUid uid, TileRef tile)
    {
        Color color;
        // Prefer puddle solution color if present
        if (_puddles.TryGetPuddle(tile, out var puddle) && TryComp(puddle, out PuddleComponent? puddleComp) && puddleComp.Solution != null)
        {
            var sol = puddleComp.Solution.Value.Comp.Solution;
            color = sol.GetColor(_protos);
        }
        else
        {
            // FloorWaterEntity fallback -> use Water reagent color
            color = _protos.Index<ReagentPrototype>("Water").SubstanceColor;
        }

        // Tint light only on server; sprite tint is clientside visualizer concern
        _lights.SetColor(uid, color);
        _appearance.SetData(uid, CarpEggVisuals.OverlayColor, color);
    }

    private void ResetVisual(EntityUid uid)
    {
        // Reset to white
        _lights.SetColor(uid, Color.White);
        _appearance.SetData(uid, CarpEggVisuals.OverlayColor, Color.White);
    }

    private void Hatch(EntityUid uid, CarpEggComponent egg, TransformComponent xform)
    {
        // Get tile information for liquid color and reagents
        Color liquidColor = Color.White;
        Dictionary<string, FixedPoint2> rememberedReagents = new();

        if (TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            var tile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);

            // Get liquid color and reagents from puddle or FloorWaterEntity
            if (_puddles.TryGetPuddle(tile, out var puddle) && TryComp(puddle, out PuddleComponent? puddleComp) && puddleComp.Solution != null)
            {
                var sol = puddleComp.Solution.Value.Comp.Solution;
                liquidColor = sol.GetColor(_protos);

                // Remember all reagents in the solution
                foreach (var (reagentId, quantity) in sol.Contents)
                {
                    rememberedReagents[reagentId.ToString()] = quantity;
                }
            }
            else
            {
                // FloorWaterEntity fallback -> use Water reagent color
                liquidColor = _protos.Index<ReagentPrototype>("Water").SubstanceColor;
                rememberedReagents["Water"] = FixedPoint2.New(30); // Assume water
            }
        }

        // Determine spawn prototype: mostly rainbow carp, rarely holo/dungeon
        string protoId = "MobCarpServantRainbow"; // Default to rainbow

        if (egg.Queen != null && TryComp(egg.Queen.Value, out CarpQueenComponent? queen))
        {
            // Use spawn chances from queen component
            var roll = _rand.Next(100);
            var cumulative = 0;
            var selected = false;

            foreach (var (proto, chance) in queen.SpawnChances)
            {
                cumulative += chance;
                if (roll < cumulative)
                {
                    protoId = proto;
                    selected = true;
                    break;
                }
            }

            // If no spawn chance matched (sum < 100), default to rainbow
            if (!selected)
                protoId = "MobCarpServantRainbow";
        }

        var mob = Spawn(protoId, xform.Coordinates);

        // Store liquid memory
        var memory = EnsureComp<CarpServantMemoryComponent>(mob);
        memory.LiquidColor = liquidColor;
        memory.RememberedReagents = rememberedReagents;

        // Check if queen is nearby
        bool queenNearby = false;
        EntityUid? closestFriend = null;
        float closestDistance = float.MaxValue;

        if (egg.Queen != null && Exists(egg.Queen.Value))
        {
            var queenXform = Transform(egg.Queen.Value);
            var queenCoords = queenXform.Coordinates.ToMap(EntityManager, _xformSys);
            var mobCoords = xform.Coordinates.ToMap(EntityManager, _xformSys);
            var distance = (queenCoords.Position - mobCoords.Position).Length();

            // Consider queen "nearby" if within configured range
            if (distance <= egg.QueenCheckRange)
                queenNearby = true;
        }

        // Always remember nearby players (within configured range)
        var nearbyEntities = new HashSet<EntityUid>();
        _lookup.GetEntitiesInRange(xform.Coordinates, egg.FriendSearchRange, nearbyEntities);

        var exception = EnsureComp<FactionExceptionComponent>(mob);

        foreach (var entity in nearbyEntities)
        {
            // Check if it's a humanoid (same as MobTomatoKiller uses whitelist with HumanoidAppearanceComponent)
            // This will match both players and AI with humanoid appearance
            if (HasComp<HumanoidAppearanceComponent>(entity))
            {
                memory.RememberedFriends.Add(entity);

                // Add to faction exceptions so they won't be attacked (unless queen orders)
                // Use NpcFactionSystem to properly add to ignored list
                if (!_npcFaction.IsIgnored((mob, exception), entity))
                {
                    _npcFaction.IgnoreEntity((mob, exception), (entity, null));
                }

                // Track closest friend for following
                var entityXform = Transform(entity);
                var entityCoords = entityXform.Coordinates.ToMap(EntityManager, _xformSys);
                var mobCoords = xform.Coordinates.ToMap(EntityManager, _xformSys);
                var friendDistance = (entityCoords.Position - mobCoords.Position).Length();

                if (friendDistance < closestDistance)
                {
                    closestDistance = friendDistance;
                    closestFriend = entity;
                }
            }
        }

        // If queen is nearby, make carp a servant; otherwise, let it work as normal carp
        if (queenNearby && egg.Queen != null && Exists(egg.Queen.Value))
        {
            // Make carp a servant of the queen
            if (TryComp(egg.Queen, out CarpQueenComponent? qc))
            {
                var queenUid = egg.Queen.Value;
                memory.RememberedFriends.Add(queenUid);
                if (!_npcFaction.IsIgnored((mob, exception), queenUid))
                {
                    _npcFaction.IgnoreEntity((mob, exception), (queenUid, null));
                }

                var comp = EnsureComp<CarpQueenServantComponent>(mob);
                comp.Queen = egg.Queen;
                Dirty(mob, comp);
                qc.Servants.Add(mob);
                // Remove egg from tracking
                qc.Eggs.Remove(uid);

                // Follow queen and execute her commands
                _npc.SetBlackboard(mob, NPCBlackboard.FollowTarget, new EntityCoordinates(egg.Queen.Value, Vector2.Zero));
                _carpQueenSystem.UpdateServantNpc(mob, qc.CurrentOrder);
            }
        }
        else
        {
            // Queen is not nearby - carp works as normal carp (like MobTomatoKiller)
            // Remove servant components if they exist
            RemComp<CarpQueenServantComponent>(mob);

            // Use normal carp HTN compound instead of RatServantCompound
            if (TryComp<HTNComponent>(mob, out var htn))
            {
                // Change HTN root task to normal carp behavior
                htn.RootTask = new HTNCompoundTask { Task = "DragonCarpCompound" };
                _htn.Replan(htn);
            }

            // Set follow target to closest friend if available
            if (closestFriend != null)
            {
                _npc.SetBlackboard(mob, NPCBlackboard.FollowTarget, new EntityCoordinates(closestFriend.Value, Vector2.Zero));
            }

            // Still remove egg from tracking if queen exists
            if (TryComp(egg.Queen, out CarpQueenComponent? qc))
            {
                qc.Eggs.Remove(uid);
            }
        }

        // Apply color to carp (will be handled by visualizer system)
        Dirty(mob, memory);
        QueueDel(uid);
    }

    // Public entry-point for other systems (e.g. queen) to re-check hatching conditions
    public void RequestHatchCheck(EntityUid uid)
    {
        if (!TryComp(uid, out CarpEggComponent? egg))
            return;

        TryHatchCheck(uid, egg);
    }

    private void OnEggDestroyed(EntityUid uid, CarpEggComponent egg, DestructionEventArgs args)
    {
        // Spill 2u of a random reagent on destruction
        var reagents = _protos.EnumeratePrototypes<ReagentPrototype>();
        string chosen = null!;
        var count = 0;
        foreach (var r in reagents)
        {
            count++;
            if (_rand.Prob(1f / count))
                chosen = r.ID;
        }

        if (chosen != null)
        {
            var sol = new Solution(chosen, FixedPoint2.New(2));
            _puddles.TrySpillAt(uid, sol, out _, sound: false);
        }

        // Remove egg from queen tracking
        if (egg.Queen != null && TryComp(egg.Queen.Value, out CarpQueenComponent? queen))
        {
            queen.Eggs.Remove(uid);
        }
    }

    private void OnEggShutdown(EntityUid uid, CarpEggComponent egg, ComponentShutdown args)
    {
        if (egg.Queen != null && TryComp(egg.Queen.Value, out CarpQueenComponent? queen))
        {
            queen.Eggs.Remove(uid);
        }
    }
}


