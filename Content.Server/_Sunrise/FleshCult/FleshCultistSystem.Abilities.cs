using System.Linq;
using System.Numerics;
using Content.Server.Construction.Components;
using Content.Shared._Sunrise.FleshCult;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Cuffs.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.FleshCult;

public sealed partial class FleshCultistSystem
{
    private void InitializeAbilities()
    {
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistBladeActionEvent>(OnBladeActionEvent);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistClawActionEvent>(OnClawActionEvent);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistFistActionEvent>(OnFistActionEvent);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistSpikeHandGunActionEvent>(OnSpikeHandGunActionEvent);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistArmorActionEvent>(OnArmorActionEvent);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistHeavyArmorActionEvent>(OnHeavyArmorActionEvent);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistSpiderLegsActionEvent>(OnSpiderLegsActionEvent);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistAdrenalinActionEvent>(OnAdrenalinActionEvent);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistCreateFleshHeartActionEvent>(OnCreateFleshHeartActionEvent);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistThrowHuggerActionEvent>(OnThrowHugger);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistAcidSpitActionEvent>(OnAcidSpit);
    }

    private void OnAcidSpit(EntityUid uid, FleshCultistComponent component, FleshCultistAcidSpitActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var acidBullet = Spawn(component.BulletAcidSpawnId, Transform(uid).Coordinates);
        var xform = Transform(uid);
        var mapCoords = args.Target.ToMap(_entityManager, _transformSystem);
        var direction = mapCoords.Position - xform.MapPosition.Position;
        var userVelocity = _physics.GetMapLinearVelocity(uid);

        _gunSystem.ShootProjectile(acidBullet, direction, userVelocity, uid, uid);
        _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
    }

    private void OnBladeActionEvent(EntityUid uid, FleshCultistComponent component, FleshCultistBladeActionEvent args)
    {
        if (args.Handled)
            return;
        if (TryComp<CuffableComponent>(uid, out var cuffableComponent))
        {
            if (cuffableComponent.CuffedHandCount > 0)
                _cuffable.Uncuff(uid, uid, cuffableComponent.LastAddedCuffs);
        }
        var hands = _handsSystem.EnumerateHands(uid);
        var enumerateHands = hands as Hand[] ?? Enumerable.ToArray<Hand>(hands);
        foreach (var enumerateHand in enumerateHands)
        {
            if (enumerateHand.Container == null)
                continue;
            foreach (var containerContainedEntity in enumerateHand.Container.ContainedEntities)
            {
                if (!TryComp(containerContainedEntity, out MetaDataComponent? metaData))
                    continue;
                if (metaData.EntityPrototype == null)
                    continue;
                if (!HasComp<_Sunrise.FleshCult.FleshHandModComponent>(containerContainedEntity))
                {
                    if (enumerateHand != enumerateHands.First())
                        continue;
                    var isDrop = _handsSystem.TryDrop(uid, checkActionBlocker: false);
                    if (metaData.EntityPrototype.ID == component.BladeSpawnId)
                        continue;
                    if (isDrop)
                        continue;
                    _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-user-hand-blocked"),
                        uid, uid, PopupType.Large);
                    return;
                }
                {
                    if (enumerateHand != enumerateHands.First())
                    {
                        if (metaData.EntityPrototype.ID != component.BladeSpawnId)
                            continue;
                        QueueDel(containerContainedEntity);
                    }
                    else
                    {
                        if (metaData.EntityPrototype.ID != component.BladeSpawnId)
                        {
                            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-user-hand-blocked"),
                                uid, uid, PopupType.Large);
                        }
                        else
                        {
                            QueueDel(containerContainedEntity);
                            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-blade-in-hand",
                                ("Entity", uid)), uid, PopupType.LargeCaution);
                            _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                            EnsureComp<CuffableComponent>(uid);
                            args.Handled = true;
                        }

                        return;
                    }
                }
            }
        }

        var blade = Spawn(component.BladeSpawnId, Transform(uid).Coordinates);
        var isPickup = _handsSystem.TryPickup(uid, blade, checkActionBlocker: false,
            animateUser: false, animate: false);
        if (isPickup)
        {
            _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-hand-in-blade", ("Entity", uid)),
                uid, PopupType.LargeCaution);
            if (HasComp<CuffableComponent>(uid))
            {
                EntityManager.RemoveComponent<CuffableComponent>(uid);
            }
        }
        else
        {
            Logger.Error("Failed to equip blade to hand, removing blade");
            QueueDel(blade);
        }
        args.Handled = true;
    }

    private void OnClawActionEvent(EntityUid uid, FleshCultistComponent component, FleshCultistClawActionEvent args)
    {
        if (args.Handled)
            return;
        if (TryComp<CuffableComponent>(uid, out var cuffableComponent))
        {
            if (cuffableComponent.CuffedHandCount > 0)
                _cuffable.Uncuff(uid, uid, cuffableComponent.LastAddedCuffs);
        }
        var hands = _handsSystem.EnumerateHands(uid);
        var enumerateHands = hands as Hand[] ?? Enumerable.ToArray<Hand>(hands);
        foreach (var enumerateHand in enumerateHands)
        {
            if (enumerateHand.Container == null)
                continue;
            foreach (var containerContainedEntity in enumerateHand.Container.ContainedEntities)
            {
                if (!TryComp(containerContainedEntity, out MetaDataComponent? metaData))
                    continue;
                if (metaData.EntityPrototype == null)
                    continue;
                if (!HasComp<_Sunrise.FleshCult.FleshHandModComponent>(containerContainedEntity))
                {
                    if (enumerateHand != enumerateHands.First())
                        continue;
                    var isDrop = _handsSystem.TryDrop(uid, checkActionBlocker: false);
                    if (metaData.EntityPrototype.ID == component.ClawSpawnId)
                        continue;
                    if (isDrop)
                        continue;
                    _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-user-hand-blocked"),
                        uid, uid, PopupType.Large);
                    return;
                }
                {
                    if (enumerateHand != enumerateHands.First())
                    {
                        if (metaData.EntityPrototype.ID != component.ClawSpawnId)
                            continue;
                        QueueDel(containerContainedEntity);
                    }
                    else
                    {
                        if (metaData.EntityPrototype.ID != component.ClawSpawnId)
                        {
                            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-user-hand-blocked"),
                                uid, uid, PopupType.Large);
                        }
                        else
                        {
                            QueueDel(containerContainedEntity);
                            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-claw-in-hand",
                                ("Entity", uid)), uid, PopupType.LargeCaution);
                            _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                            EnsureComp<CuffableComponent>(uid);
                            args.Handled = true;
                        }
                        return;
                    }
                }
            }
        }

        var claw = Spawn(component.ClawSpawnId, Transform(uid).Coordinates);
        var isPickup = _handsSystem.TryPickup(uid, claw, checkActionBlocker: false,
            animateUser: false, animate: false);
        if (isPickup)
        {
            _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-hand-in-claw", ("Entity", uid)),
                uid, PopupType.LargeCaution);
            if (HasComp<CuffableComponent>(uid))
                EntityManager.RemoveComponent<CuffableComponent>(uid);
        }
        else
        {
            QueueDel(claw);
        }
        args.Handled = true;
    }


    private void OnFistActionEvent(EntityUid uid, FleshCultistComponent component, FleshCultistFistActionEvent args)
    {
        if (args.Handled)
            return;
        if (TryComp<CuffableComponent>(uid, out var cuffableComponent))
        {
            if (cuffableComponent.CuffedHandCount > 0)
                _cuffable.Uncuff(uid, uid, cuffableComponent.LastAddedCuffs);
        }
        var hands = _handsSystem.EnumerateHands(uid);
        var enumerateHands = hands as Hand[] ?? Enumerable.ToArray<Hand>(hands);
        foreach (var enumerateHand in enumerateHands)
        {
            if (enumerateHand.Container == null)
                continue;
            foreach (var containerContainedEntity in enumerateHand.Container.ContainedEntities)
            {
                if (!TryComp(containerContainedEntity, out MetaDataComponent? metaData))
                    continue;
                if (metaData.EntityPrototype == null)
                    continue;
                if (!HasComp<_Sunrise.FleshCult.FleshHandModComponent>(containerContainedEntity))
                {
                    if (enumerateHand != enumerateHands.First())
                        continue;
                    var isDrop = _handsSystem.TryDrop(uid, checkActionBlocker: false);
                    if (metaData.EntityPrototype.ID == component.FistSpawnId)
                        continue;
                    if (isDrop)
                        continue;
                    _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-user-hand-blocked"),
                        uid, uid, PopupType.Large);
                    return;
                }
                {
                    if (enumerateHand != enumerateHands.First())
                    {
                        if (metaData.EntityPrototype.ID != component.FistSpawnId)
                            continue;
                        QueueDel(containerContainedEntity);
                    }
                    else
                    {
                        if (metaData.EntityPrototype.ID != component.FistSpawnId)
                        {
                            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-user-hand-blocked"),
                                uid, uid, PopupType.Large);
                        }
                        else
                        {
                            QueueDel(containerContainedEntity);
                            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-claw-in-hand",
                                ("Entity", uid)), uid, PopupType.LargeCaution);
                            _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                            EnsureComp<CuffableComponent>(uid);
                            args.Handled = true;
                        }
                        return;
                    }
                }
            }
        }

        var fist = Spawn(component.FistSpawnId, Transform(uid).Coordinates);
        var isPickup = _handsSystem.TryPickup(uid, fist, checkActionBlocker: false,
            animateUser: false, animate: false);
        if (isPickup)
        {
            _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-hand-in-claw", ("Entity", uid)),
                uid, PopupType.LargeCaution);
            if (HasComp<CuffableComponent>(uid))
                EntityManager.RemoveComponent<CuffableComponent>(uid);
        }
        else
        {
            QueueDel(fist);
        }
        args.Handled = true;
    }

    private void OnSpikeHandGunActionEvent(EntityUid uid, FleshCultistComponent component, FleshCultistSpikeHandGunActionEvent args)
    {
        if (args.Handled)
            return;
        if (TryComp<CuffableComponent>(uid, out var cuffableComponent))
        {
            if (cuffableComponent.CuffedHandCount > 0)
                _cuffable.Uncuff(uid, uid, cuffableComponent.LastAddedCuffs);
        }
        var hands = _handsSystem.EnumerateHands(uid);
        var enumerateHands = hands as Hand[] ?? Enumerable.ToArray<Hand>(hands);
        foreach (var enumerateHand in enumerateHands)
        {
            if (enumerateHand.Container == null)
                continue;
            foreach (var containerContainedEntity in enumerateHand.Container.ContainedEntities)
            {
                if (!TryComp(containerContainedEntity, out MetaDataComponent? metaData))
                    continue;
                if (metaData.EntityPrototype == null)
                    continue;
                if (!HasComp<_Sunrise.FleshCult.FleshHandModComponent>(containerContainedEntity))
                {
                    if (enumerateHand != enumerateHands.First())
                        continue;
                    var isDrop = _handsSystem.TryDrop(uid, checkActionBlocker: false);
                    if (metaData.EntityPrototype.ID == component.SpikeHandGunSpawnId)
                        continue;
                    if (isDrop)
                        continue;
                    _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-user-hand-blocked"),
                        uid, uid, PopupType.Large);
                    return;
                }
                {
                    if (enumerateHand != enumerateHands.First())
                    {
                        if (metaData.EntityPrototype.ID != component.SpikeHandGunSpawnId)
                            continue;
                        QueueDel(containerContainedEntity);
                    }
                    else
                    {
                        if (metaData.EntityPrototype.ID != component.SpikeHandGunSpawnId)
                        {
                            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-user-hand-blocked"),
                                uid, uid, PopupType.Large);
                        }
                        else
                        {
                            QueueDel(containerContainedEntity);
                            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-spike-gun-in-hand",
                                ("Entity", uid)), uid, PopupType.LargeCaution);
                            _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                            EnsureComp<CuffableComponent>(uid);
                            args.Handled = true;
                        }
                        return;
                    }
                }
            }
        }

        var claw = Spawn(component.SpikeHandGunSpawnId, Transform(uid).Coordinates);
        var isPickup = _handsSystem.TryPickup(uid, claw, checkActionBlocker: false,
            animateUser: false, animate: false);
        if (isPickup)
        {
            _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-hand-in-spike-gun", ("Entity", uid)),
                uid, PopupType.LargeCaution);
            if (HasComp<CuffableComponent>(uid))
                RemComp<CuffableComponent>(uid);
        }
        else
        {
            QueueDel(claw);
        }
        args.Handled = true;
    }

    private void OnArmorActionEvent(EntityUid uid, FleshCultistComponent component, FleshCultistArmorActionEvent args)
    {
        _inventory.TryGetSlotEntity(uid, "outerClothing", out var outerClothing);
        if (outerClothing != null)
        {
            if (!TryComp(outerClothing, out MetaDataComponent? metaData))
                return;
            if (metaData.EntityPrototype == null)
                return;
            if (metaData.EntityPrototype.ID == component.HeavyArmorSpawnId)
            {
                _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-armor-blocked"),
                    uid, uid, PopupType.Large);
            }
            else if (metaData.EntityPrototype.ID != component.ArmorSpawnId)
            {
                _inventory.TryUnequip(uid, "outerClothing", true, true);
                var armor = Spawn(component.ArmorSpawnId, Transform(uid).Coordinates);
                var equipped = _inventory.TryEquip(uid, armor, "outerClothing", true);
                if (!equipped)
                {
                    QueueDel(armor);
                }
                else
                {
                    _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                    _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-armor-on",
                        ("Entity", uid)), uid, PopupType.LargeCaution);
                    args.Handled = true;
                }
            }
            else
            {
                _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-armor-off",
                    ("Entity", uid)), uid, PopupType.LargeCaution);
                EntityManager.DeleteEntity(outerClothing.Value);
                _movement.RefreshMovementSpeedModifiers(uid);
                args.Handled = true;
            }
        }
        else
        {
            var armor = Spawn(component.ArmorSpawnId, Transform(uid).Coordinates);
            var equipped = _inventory.TryEquip(uid, armor, "outerClothing", true);
            if (!equipped)
            {
                QueueDel(armor);
            }
            else
            {
                _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-armor-on",
                        ("Entity", uid)), uid, PopupType.LargeCaution);
                args.Handled = true;
            }
        }
    }

    private void OnHeavyArmorActionEvent(EntityUid uid, FleshCultistComponent component, FleshCultistHeavyArmorActionEvent args)
    {
        _inventory.TryGetSlotEntity(uid, "shoes", out var shoes);
        if (shoes != null)
        {
            if (!TryComp(shoes, out MetaDataComponent? metaData))
                return;
            if (metaData.EntityPrototype == null)
                return;
            if (metaData.EntityPrototype.ID == component.SpiderLegsSpawnId)
            {
                _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-armor-blocked"),
                    uid, uid, PopupType.Large);
                return;
            }
        }
        _inventory.TryGetSlotEntity(uid, "outerClothing", out var outerClothing);
        if (outerClothing != null)
        {
            if (!TryComp(outerClothing, out MetaDataComponent? metaData))
                return;
            if (metaData.EntityPrototype == null)
                return;
            if (metaData.EntityPrototype.ID == component.ArmorSpawnId)
            {
                _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-armor-blocked"),
                    uid, uid, PopupType.Large);
            }
            else if (metaData.EntityPrototype.ID != component.HeavyArmorSpawnId)
            {
                _inventory.TryUnequip(uid, "outerClothing", true, true);
                var armor = Spawn(component.HeavyArmorSpawnId, Transform(uid).Coordinates);
                var equipped = _inventory.TryEquip(uid, armor, "outerClothing", true);
                if (!equipped)
                {
                    QueueDel(armor);
                }
                else
                {
                    _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                    _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-armor-on",
                        ("Entity", uid)), uid, PopupType.LargeCaution);
                    args.Handled = true;
                }
            }
            else
            {
                _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-armor-off",
                    ("Entity", uid)), uid, PopupType.LargeCaution);
                EntityManager.DeleteEntity(outerClothing.Value);
                _movement.RefreshMovementSpeedModifiers(uid);
                args.Handled = true;
            }
        }
        else
        {
            var armor = Spawn(component.HeavyArmorSpawnId, Transform(uid).Coordinates);
            var equipped = _inventory.TryEquip(uid, armor, "outerClothing", true);
            if (!equipped)
            {
                QueueDel(armor);
            }
            else
            {
                _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-armor-on",
                        ("Entity", uid)), uid, PopupType.LargeCaution);
                args.Handled = true;
            }
        }
    }

    private void OnSpiderLegsActionEvent(EntityUid uid, FleshCultistComponent component, FleshCultistSpiderLegsActionEvent args)
    {
        _inventory.TryGetSlotEntity(uid, "outerClothing", out var outerClothing);
        if (outerClothing != null)
        {
            if (!TryComp(outerClothing, out MetaDataComponent? metaData))
                return;
            if (metaData.EntityPrototype == null)
                return;
            if (metaData.EntityPrototype.ID == component.ArmorSpawnId || metaData.EntityPrototype.ID == component.HeavyArmorSpawnId)
            {
                _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-spider-legs-blocked"),
                    uid, uid, PopupType.Large);
                args.Handled = true;
                return;
            }

            if (_tagSystem.HasTag(outerClothing.Value, "FullBodyOuter"))
            {
                _inventory.TryUnequip(uid, "outerClothing", true, true);
                args.Handled = true;
            }
        }
        _inventory.TryGetSlotEntity(uid, "shoes", out var shoes);
        if (shoes != null)
        {
            if (!TryComp(shoes, out MetaDataComponent? metaData))
                return;
            if (metaData.EntityPrototype == null)
                return;

            if (metaData.EntityPrototype.ID == component.SpiderLegsSpawnId)
            {
                _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-spider-legs-off",
                    ("Entity", uid)), uid, PopupType.LargeCaution);
                EntityManager.DeleteEntity(shoes.Value);
                _movement.RefreshMovementSpeedModifiers(uid);
                args.Handled = true;
                return;
            }

            _inventory.TryUnequip(uid, "shoes", true, true);
        }
        _inventory.TryUnequip(uid, "socks", true, true);
        var legs = Spawn(component.SpiderLegsSpawnId, Transform(uid).Coordinates);
        var equipped = _inventory.TryEquip(uid, legs, "shoes", true, true);
        if (!equipped)
        {
            QueueDel(legs);
        }
        else
        {
            _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-transform-spider-legs-on",
                ("Entity", uid)), uid, PopupType.LargeCaution);
            args.Handled = true;
        }
    }

    private void OnAdrenalinActionEvent(EntityUid uid, FleshCultistComponent component, FleshCultistAdrenalinActionEvent args)
    {
        if (!_solutionContainerSystem.TryGetInjectableSolution(uid, out var injectableSolution, out var _))
            return;
        var transferSolution = new Solution();
        foreach (var solution in component.AdrenalinReagents)
        {
            transferSolution.AddReagent(solution.Reagent, solution.Quantity);
        }
        _solutionContainerSystem.TryAddSolution(injectableSolution.Value, transferSolution);
        args.Handled = true;
    }

    private void OnCreateFleshHeartActionEvent(EntityUid uid, FleshCultistComponent component, FleshCultistCreateFleshHeartActionEvent args)
    {
        var xform = Transform(uid);
        var radius = 1.5f;
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-cant-spawn-flesh-heart-in-space",
                ("Entity", uid)), uid, PopupType.Large);
            return;
        }

        var offsetValue = Vector2Helpers.Normalized(xform.LocalRotation.ToWorldVec());
        var targetCord = xform.Coordinates.Offset(offsetValue).SnapToGrid(EntityManager);
        var tilerefs = Enumerable.ToArray<TileRef>(grid.GetLocalTilesIntersecting(
            new Box2(targetCord.Position + new Vector2(-radius, -radius), targetCord.Position + new Vector2(radius, radius))));
        foreach (var tileref in tilerefs)
        {
            foreach (var entity in tileref.GetEntitiesInTile())
            {
                PhysicsComponent? physics = null; // We use this to check if it's impassable
                if (HasComp<MobStateComponent>(entity) && entity != uid || // Is it a mob?
                    Resolve(entity, ref physics, false) && (physics.CollisionLayer & (int) CollisionGroup.Impassable) != 0 ||
                    HasComp<ConstructionComponent>(entity) && entity != uid) // Is construction?
                {
                    _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-cant-spawn-flesh-heart-here",
                        ("Entity", uid)), uid, PopupType.Large);
                    return;
                }
            }
        }
        _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
        EntityManager.SpawnEntity(component.FleshHeartId, targetCord);
        args.Handled = true;
    }

    private void OnThrowHugger(EntityUid uid, FleshCultistComponent component, FleshCultistThrowHuggerActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var hugger = Spawn(component.HuggerMobSpawnId, Transform(uid).Coordinates);
        var xform = Transform(uid);
        var mapCoords = args.Target.ToMap(_entityManager, _transformSystem);
        var direction = mapCoords.Position - xform.MapPosition.Position;

        _throwing.TryThrow(hugger, direction, 7F, uid, 10F);
        if (component.SoundThrowHugger != null)
        {
            _audioSystem.PlayPvs(component.SoundThrowHugger, uid, component.SoundThrowHugger.Params);
        }
        _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-throw-hugger"), uid, uid,
            PopupType.LargeCaution);
        _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-throw-hugger-others", ("Entity", uid)),
            uid, Filter.PvsExcept(uid), true, PopupType.LargeCaution);
    }

}
