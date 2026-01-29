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
            if (!egg.Eligible)
            {
                egg.Accum += frameTime;
                egg.WaitElapsed += frameTime;
                if (egg.Accum >= egg.CheckInterval)
                {
                    egg.Accum = 0f;
                    TryHatchCheck(uid, egg);
                }

                if (egg.WaitElapsed >= egg.MaxWaitWithoutLiquid)
                {
                    _destructible.DestroyEntity(uid);
                    continue;
                }
                continue;
            }

            egg.Accum += frameTime;
            if (egg.Accum >= egg.HatchDelay)
            {
                if (TryComp<MapGridComponent>(xform.GridUid, out var grid))
                {
                    var tile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
                    if (HasSufficientLiquid(tile, egg.RequiredVolume))
                    {
                        Hatch(uid, egg, xform);
                        continue;
                    }
                }

                egg.Eligible = false;
                egg.Accum = 0f;
                ResetVisual(uid);
            }
        }
    }

    private void OnServantStartup(EntityUid uid, CarpQueenServantComponent servant, ComponentStartup args)
    {
        if (servant.Queen != null && TryComp(servant.Queen.Value, out CarpQueenComponent? queen))
        {
            _npc.SetBlackboard(uid, NPCBlackboard.FollowTarget, new EntityCoordinates(servant.Queen.Value, Vector2.Zero));
            var ratKingOrder = SharedCarpQueenSystem.ConvertToRatKingOrder(queen.CurrentOrder);
            _npc.SetBlackboard(uid, NPCBlackboard.CurrentOrders, ratKingOrder);
            _npc.SetBlackboard(uid, "FollowCloseRange", 1.0f);
            _npc.SetBlackboard(uid, "FollowRange", 1.5f);
        }
        else
        {
            _npc.SetBlackboard(uid, NPCBlackboard.CurrentOrders, RatKingOrderType.Loose);
        }

        if (TryComp<HTNComponent>(uid, out var htn))
        {
            if (htn.Plan != null)
                _htn.ShutdownPlan(htn);
            _htn.Replan(htn);
        }
    }

    private void OnEggMapInit(EntityUid uid, CarpEggComponent egg, MapInitEvent args)
    {
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
        if (!TryComp<PuddleComponent>(args.Solution.Owner, out var _))
            return;

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
        if (_puddles.TryGetPuddle(tile, out var puddle))
        {
            var vol = _puddles.CurrentVolume(puddle);
            if (vol >= FixedPoint2.New(required))
                return true;
        }

        var gridId = tile.GridUid;
        if (gridId != null)
        {
            if (gridId is { } gid && TryComp<MapGridComponent>(gid, out var grid))
            {
                var enumerator = _map.GetAnchoredEntitiesEnumerator(gid, grid, tile.GridIndices);
                while (enumerator.MoveNext(out EntityUid? ent))
                {
                    if (!ent.HasValue)
                        continue;

                    var meta = MetaData(ent.Value);
                    if (meta.EntityPrototype?.ID == "FloorWaterEntity")
                        return true;
                }
            }

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
        if (_puddles.TryGetPuddle(tile, out var puddle) && TryComp(puddle, out PuddleComponent? puddleComp) && puddleComp.Solution != null)
        {
            var sol = puddleComp.Solution.Value.Comp.Solution;
            color = sol.GetColor(_protos);
        }
        else
        {
            color = _protos.Index<ReagentPrototype>("Water").SubstanceColor;
        }

        _lights.SetColor(uid, color);
        _appearance.SetData(uid, CarpEggVisuals.OverlayColor, color);
    }

    private void ResetVisual(EntityUid uid)
    {
        _lights.SetColor(uid, Color.White);
        _appearance.SetData(uid, CarpEggVisuals.OverlayColor, Color.White);
    }

    private void Hatch(EntityUid uid, CarpEggComponent egg, TransformComponent xform)
    {
        Color liquidColor = Color.White;
        List<Color> liquidColors = new();
        Dictionary<string, FixedPoint2> rememberedReagents = new();

        if (TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            var tile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);

            var waterColor = _protos.Index<ReagentPrototype>("Water").SubstanceColor;
            liquidColors.Add(waterColor);

            if (_puddles.TryGetPuddle(tile, out var puddle) && TryComp(puddle, out PuddleComponent? puddleComp) && puddleComp.Solution != null)
            {
                var sol = puddleComp.Solution.Value.Comp.Solution;
                liquidColor = sol.GetColor(_protos);

                foreach (var (reagentId, quantity) in sol.Contents)
                {
                    rememberedReagents[reagentId.ToString()] = quantity;

                    if (_protos.TryIndex<ReagentPrototype>(reagentId.ToString(), out var reagentProto))
                    {
                        var reagentColor = reagentProto.SubstanceColor;
                        if (!liquidColors.Contains(reagentColor))
                        {
                            liquidColors.Add(reagentColor);
                        }
                    }
                }
            }
            else
            {
                liquidColor = waterColor;
                rememberedReagents["Water"] = FixedPoint2.New(30);
            }
        }

        string protoId = "MobCarpServantRainbow";

        if (egg.Queen != null && TryComp(egg.Queen.Value, out CarpQueenComponent? queen))
        {
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

            if (!selected)
                protoId = "MobCarpServantRainbow";
        }

        var mob = Spawn(protoId, xform.Coordinates);

        var memory = EnsureComp<CarpServantMemoryComponent>(mob);
        memory.LiquidColor = liquidColor;
        memory.LiquidColors = liquidColors;
        memory.RememberedReagents = rememberedReagents;
        memory.BiteReagentAmount = egg.BiteReagentAmount;

        bool queenNearby = false;
        EntityUid? closestFriend = null;
        float closestDistance = float.MaxValue;

        if (egg.Queen != null && Exists(egg.Queen.Value))
        {
            var queenXform = Transform(egg.Queen.Value);
            var queenCoords = queenXform.Coordinates.ToMap(EntityManager, _xformSys);
            var mobCoords = xform.Coordinates.ToMap(EntityManager, _xformSys);
            var distance = (queenCoords.Position - mobCoords.Position).Length();

            if (distance <= egg.QueenSearchRange)
                queenNearby = true;
        }

        var nearbyEntities = new HashSet<EntityUid>();
        _lookup.GetEntitiesInRange(xform.Coordinates, egg.FriendSearchRange, nearbyEntities);

        var exception = EnsureComp<FactionExceptionComponent>(mob);

        foreach (var entity in nearbyEntities)
        {
            if (HasComp<HumanoidAppearanceComponent>(entity))
            {
                memory.RememberedFriends.Add(entity);

                if (!_npcFaction.IsIgnored((mob, exception), entity))
                {
                    _npcFaction.IgnoreEntity((mob, exception), (entity, null));
                }

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

        if (queenNearby && egg.Queen != null && Exists(egg.Queen.Value))
        {
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
                qc.Eggs.Remove(uid);

                _npc.SetBlackboard(mob, NPCBlackboard.FollowTarget, new EntityCoordinates(egg.Queen.Value, Vector2.Zero));
                _carpQueenSystem.UpdateServantNpc(mob, qc.CurrentOrder);
            }
        }
        else
        {
            RemComp<CarpQueenServantComponent>(mob);

            if (TryComp<HTNComponent>(mob, out var htn))
            {
                htn.RootTask = new HTNCompoundTask { Task = "DragonCarpCompound" };
                _htn.Replan(htn);
            }

            if (closestFriend != null)
            {
                _npc.SetBlackboard(mob, NPCBlackboard.FollowTarget, new EntityCoordinates(closestFriend.Value, Vector2.Zero));
            }

            if (TryComp(egg.Queen, out CarpQueenComponent? qc))
            {
                qc.Eggs.Remove(uid);
            }
        }

        Dirty(mob, memory);
        QueueDel(uid);
    }

    public void RequestHatchCheck(EntityUid uid)
    {
        if (!TryComp(uid, out CarpEggComponent? egg))
            return;

        TryHatchCheck(uid, egg);
    }

    private void OnEggDestroyed(EntityUid uid, CarpEggComponent egg, DestructionEventArgs args)
    {
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


