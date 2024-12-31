using System.Linq;
using System.Numerics;
using Content.Server._Sunrise.FleshCult.FleshGrowth;
using Content.Server._Sunrise.FleshCult.GameRule;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Humanoid;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Server.Traits.Assorted;
using Content.Shared._Sunrise.FleshCult;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Flesh;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Robust.Server.Containers;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.FleshCult
{
    public sealed class FleshHeartSystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly BodySystem _body = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _sharedHuApp = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly SharedAppearanceSystem _sharedAppearance = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FleshHeartComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<FleshHeartComponent, DestructionEventArgs>(OnDestruction);
            SubscribeLocalEvent<FleshHeartComponent, DragDropTargetEvent>(HandleDragDropOn);
            SubscribeLocalEvent<FleshHeartComponent, FleshHeartDragFinished>(OnDragFinished);
            SubscribeLocalEvent<FleshHeartComponent, ComponentInit>(OnComponentInit);
        }

        private void OnShutdown(EntityUid uid, FleshHeartComponent component, ComponentShutdown args)
        {
            _audioSystem.Stop(component.AmbientAudioStream);
        }

        private void OnDestruction(EntityUid uid, FleshHeartComponent component, DestructionEventArgs args)
        {
            if (component.State != HeartStates.Base)
            {
                _audioSystem.Stop(component.AmbientAudioStream);
                //var stationUid = _stationSystem.GetOwningStation(uid);
                //if (stationUid != null)
                //{
                //    _alertLevel.SetLevel(stationUid.Value, component.AlertLevelOnDeactivate, true,
                //        true, true);
                //}
                var xform = Transform(uid);
                var coordinates = xform.Coordinates;
                foreach (var ent in component.BodyContainer.ContainedEntities.ToArray())
                {
                    _containerSystem.Remove(ent, component.BodyContainer, force: true);
                    Transform(ent).Coordinates = coordinates;
                }
                var fleshTilesQuery = EntityQueryEnumerator<SpreaderFleshComponent>();
                while (fleshTilesQuery.MoveNext(out var ent, out var comp))
                {
                    if (comp.Source != uid)
                        continue;
                    if (!TryComp<TagComponent>(ent, out var tagComponent))
                        continue;
                    if (_tagSystem.HasAllTags(tagComponent, "Wall", "Flesh"))
                        _damageableSystem.TryChangeDamage(ent, component.DamageMobsIfHeartDestruct);
                    else
                        QueueDel(ent);

                }
                var fleshWalls = new List<EntityUid>();
                var fleshWallsQuery = EntityQueryEnumerator<TagComponent>();
                while (fleshWallsQuery.MoveNext(out var ent, out var comp))
                {
                    if (!TryComp<TagComponent>(ent, out var tagComponent))
                        continue;
                    var isFleshWall = _tagSystem.HasAllTags(tagComponent, "Wall", "Flesh");
                    if (isFleshWall)
                    {
                        fleshWalls.Add(ent);
                    }
                }
                foreach (var mob in component.EdgeMobs.ToArray())
                {
                    _damageableSystem.TryChangeDamage(mob, component.DamageMobsIfHeartDestruct);
                }
                RaiseLocalEvent(new FleshHeartDestructionEvent()
                {
                    FleshHeardUid = uid,
                    OwningStation = xform.GridUid
                });
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var fleshHeartQuery = EntityQueryEnumerator<FleshHeartComponent, TransformComponent>();
            while (fleshHeartQuery.MoveNext(out var ent, out var comp, out var xform))
            {
                var fleshCultRule = EntityQuery<FleshCultRuleComponent>().FirstOrDefault();
                if (fleshCultRule == null)
                {
                    continue;
                }

                var owningStation = _stationSystem.GetOwningStation(ent, xform);

                if (owningStation != fleshCultRule.TargetStation)
                    continue;

                switch (comp.State)
                {
                    case HeartStates.Base:
                    {
                        comp.Accumulator += frameTime;

                        if (comp.Accumulator <= 1)
                            continue;

                        comp.Accumulator -= 1;
                        if (comp.BodyContainer.ContainedEntities.Count >= comp.BodyToFinalStage)
                        {
                            comp.SpawnMobsAccumulator = 500;
                            comp.State = HeartStates.Active;
                            RaiseLocalEvent(new FleshHeartActivateEvent()
                            {
                                FleshHeardUid = ent,
                                OwningStation = xform.GridUid
                            });
                            _chat.DispatchGlobalAnnouncement(
                                Loc.GetString("flesh-heart-activate-warning"),
                                colorOverride: Color.Red);
                            // _audioSystem.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), true);
                            var stationUid = _stationSystem.GetOwningStation(ent);
                            //if (stationUid != null)
                            //{
                            //    _alertLevel.SetLevel(stationUid.Value, comp.AlertLevelOnActivate, false,
                            //        true, true, true);
                            //}
                            //_audioSystem.PlayGlobal(
                            //    "/Audio/_Sunrise/FleshCult/flesh_heart_activate.ogg", Filter.Broadcast(), true,
                            //    AudioParams.Default);
                            SpawnFleshFloorOnOpenTiles(ent, comp, Transform(ent), 1);
                            _roundEndSystem.CancelRoundEndCountdown(stationUid);
                            _audioSystem.PlayPvs(comp.TransformSound, ent, comp.TransformSound.Params);
                            comp.AmbientAudioStream = _audioSystem.PlayGlobal(
                                "/Audio/_Sunrise/FleshCult/flesh_heart.ogg", Filter.Broadcast(), true,
                                AudioParams.Default.WithLoop(true).WithVolume(-3f))!.Value.Entity;
                            _appearance.SetData(ent, FleshHeartVisuals.State, FleshHeartStatus.Active);
                        }
                        break;
                    }
                    case HeartStates.Active:
                    {
                        comp.SpawnMobsAccumulator += frameTime;
                        comp.SpawnObjectsAccumulator += frameTime;
                        comp.FinalStageAccumulator += frameTime;
                        if (comp.SpawnMobsAccumulator >= comp.SpawnMobsFrequency)
                        {
                            comp.SpawnMobsAccumulator = 0;
                            SpawnMonstersOnOpenTiles(comp, xform, comp.SpawnMobsAmount, comp.SpawnMobsRadius);
                        }

                        if (comp.SpawnObjectsAccumulator >= comp.SpawnObjectsFrequency)
                        {
                            comp.SpawnObjectsAccumulator = 0;
                            // SpawnObjectsOnOpenTiles(comp, xform, comp.SpawnObjectsAmount, comp.SpawnObjectsRadius);
                        }

                        if (comp.FinalStageAccumulator >= comp.TimeLiveFinalHeartToWin)
                        {
                            comp.State = HeartStates.Disable;
                            RaiseLocalEvent(new FleshHeartFinalEvent()
                            {
                                FleshHeardUid = ent,
                                OwningStation = owningStation,
                            });
                        }
                        break;
                    }
                    case HeartStates.Disable:
                    {
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #region Interaction

        private void HandleDragDropOn(EntityUid uid, FleshHeartComponent component, ref DragDropTargetEvent args)
        {

            if (!CanAbsorb(uid, args.Dragged, component))
            {
                _popup.PopupEntity(Loc.GetString("flesh-heart-cant-absorb-targer"),
                    args.User, PopupType.Large);
                return;
            }

            if (!TryComp<FixturesComponent>(args.Dragged, out var fixturesComponent))
            {
                _popup.PopupEntity(Loc.GetString("flesh-heart-cant-absorb-targer"),
                    args.User, PopupType.Large);
                return;
            }

            if (fixturesComponent.Fixtures["fix1"].Density <= 60)
            {
                _popup.PopupEntity(
                    Loc.GetString("flesh-heart-cant-absorb-targer"),
                    uid, PopupType.Large);
                return;
            }

            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.EntryDelay, new FleshHeartDragFinished(), uid, target: args.Dragged, used: uid)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = false,
            };
            _doAfterSystem.TryStartDoAfter(doAfterArgs);
            args.Handled = true;
        }

        protected void OnComponentInit(EntityUid uid, FleshHeartComponent cryoPodComponent, ComponentInit args)
        {
            cryoPodComponent.BodyContainer = _containerSystem.EnsureContainer<Container>(uid, "bodyContainer");
        }

        private void OnDragFinished(EntityUid uid, FleshHeartComponent component, FleshHeartDragFinished args)
        {
            if (args.Cancelled || args.Handled || args.Args.Target == null)
                return;

            if (!TryComp<FixturesComponent>(args.Args.Target.Value, out var fixturesComponent))
            {
                _popup.PopupEntity(Loc.GetString("flesh-heart-cant-absorb-targer"),
                    args.User, PopupType.Large);
                return;
            }

            var xform = Transform(args.Args.Target.Value);

            if (TryComp(args.Args.Target.Value, out ContainerManagerComponent? container))
            {
                foreach (var cont in container.GetAllContainers().ToArray())
                {
                    foreach (var ent in cont.ContainedEntities.ToArray())
                    {
                        {
                            if (HasComp<BodyPartComponent>(ent))
                            {
                                continue;
                            }
                            _containerSystem.Remove(ent, cont, force: true);
                            Transform(ent).Coordinates = xform.Coordinates;
                        }
                    }
                }
            }

            if (TryComp<HumanoidAppearanceComponent>(args.Args.Target.Value, out var HuAppComponent))
            {
                if (TryComp<BodyComponent>(args.Args.Target.Value, out var bodyComponent))
                {
                    var parts = _body.GetBodyChildren(args.Args.Target.Value, bodyComponent).ToArray();

                    foreach (var part in parts)
                    {
                        if (part.Component.PartType == BodyPartType.Head)
                            continue;

                        if (part.Component.PartType == BodyPartType.Torso)
                        {
                            foreach (var organ in _body.GetPartOrgans(part.Id, part.Component))
                            {
                                _body.RemoveOrgan(organ.Id);
                            }
                        }
                        else
                        {
                            QueueDel(part.Id);
                        }
                    }
                }

                _bloodstreamSystem.TryModifyBloodLevel(args.Args.Target.Value, -300);

                var skeletonSprites = _proto.Index<HumanoidSpeciesBaseSpritesPrototype>("MobSkeletonSprites");
                foreach (var (key, id) in skeletonSprites.Sprites)
                {
                    if (key != HumanoidVisualLayers.Head)
                    {
                        _sharedHuApp.SetBaseLayerId(args.Args.Target.Value, key, id, humanoid: HuAppComponent);
                    }
                }

                _physics.SetDensity(args.Args.Target.Value, "fix1", fixturesComponent.Fixtures["fix1"], 50);

                if (TryComp<AppearanceComponent>(args.Args.Target.Value, out var appComponent))
                {
                    _sharedAppearance.SetData(args.Args.Target.Value, DamageVisualizerKeys.Disabled, true, appComponent);
                    _damageableSystem.TryChangeDamage(args.Args.Target.Value,
                        new DamageSpecifier() { DamageDict = { { "Slash", 100 } } });
                }

                EnsureComp<UnrevivableComponent>(args.Args.Target.Value);

                _containerSystem.Insert(args.Args.Target.Value, component.BodyContainer, force: true);
                _audioSystem.PlayPvs(component.TransformSound, uid, component.TransformSound.Params);
            }

            args.Handled = true;
        }

        #endregion

        private bool CanAbsorb(EntityUid uid, EntityUid dragged, FleshHeartComponent component)
        {
            if (!TryComp<MobStateComponent>(dragged, out var stateComponent))
                return false;

            if (stateComponent.CurrentState != MobState.Dead)
                return false;

            if (!Transform(uid).Anchored)
                return false;

            if (!TryComp<HumanoidAppearanceComponent>(dragged, out var humanoidAppearance))
                return false;

            if (!(component.SpeciesWhitelist.Contains(humanoidAppearance.Species)))
                return false;

            return !TryComp<MindContainerComponent>(dragged, out var mindComp) || true;
        }

        private void SpawnFleshFloorOnOpenTiles(EntityUid fleshHeart, FleshHeartComponent component, TransformComponent xform, float radius)
        {
            if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
                return;

            var localpos = xform.Coordinates.Position;
            var tilerefs = grid.GetLocalTilesIntersecting(
                new Box2(localpos + new Vector2(-radius, -radius), localpos + new Vector2(radius, radius))).ToArray();
            foreach (var tileref in tilerefs)
            {
                var canSpawnFloor = true;
                foreach (var ent in grid.GetAnchoredEntities(tileref.GridIndices).ToList())
                {
                    if (_tagSystem.HasAnyTag(ent, "Wall", "Window", "Flesh"))
                        canSpawnFloor = false;
                }
                if (canSpawnFloor)
                {
                    var location = _mapSystem.ToCenterCoordinates(tileref, grid);
                    var fleshTile = EntityManager.SpawnEntity(component.FleshTileId, location);
                    var spreaderFleshComponent = EnsureComp<SpreaderFleshComponent>(fleshTile);
                    spreaderFleshComponent.Source = fleshHeart;
                }
            }
        }

        private void SpawnMonstersOnOpenTiles(FleshHeartComponent component, TransformComponent xform, int amount, float radius)
        {
            if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
                return;

            var localpos = xform.Coordinates.Position;
            var tilerefs = grid.GetLocalTilesIntersecting(
                new Box2(localpos + new Vector2(-radius, -radius), localpos + new Vector2(radius, radius))).ToArray();
            _random.Shuffle(tilerefs);
            var physQuery = GetEntityQuery<PhysicsComponent>();
            var amountCounter = 0;
            foreach (var tileref in tilerefs)
            {
                var valid = true;
                foreach (var ent in grid.GetAnchoredEntities(tileref.GridIndices))
                {
                    if (!physQuery.TryGetComponent(ent, out var body))
                        continue;
                    if (body.BodyType != BodyType.Static ||
                        !body.Hard ||
                        (body.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
                        continue;
                    valid = false;
                    break;
                }
                if (!valid)
                    continue;
                amountCounter++;

                var randomMob = _random.Pick(component.Spawns);
                var location = _mapSystem.ToCenterCoordinates(tileref, grid);
                var mob = Spawn(randomMob, location);
                component.EdgeMobs.Add(mob);
                if (amountCounter >= amount)
                    return;
            }
        }

        public sealed class FleshHeartFinalEvent : EntityEventArgs
        {
            public EntityUid FleshHeardUid;
            public EntityUid? OwningStation;
        }

        public sealed class FleshHeartActivateEvent : EntityEventArgs
        {
            public EntityUid FleshHeardUid;
            public EntityUid? OwningStation;
        }

        public sealed class FleshHeartDestructionEvent : EntityEventArgs
        {
            public EntityUid FleshHeardUid;
            public EntityUid? OwningStation;
        }
    }
}
