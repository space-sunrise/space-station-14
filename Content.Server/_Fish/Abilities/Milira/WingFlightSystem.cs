using System;
using Content.Server.Actions;
using Content.Server.Humanoid;
using Content.Server.Popups;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Toggleable;
using Content.Shared._Fish.Abilities.Milira;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Fish.Abilities.Milira;

/// <summary>
/// Серверная система для полёта расы милира, оно использует другую систему для изменения масштаба крыльев, а также изменяет маркинг, и тратит стамину.
/// Также включает функциональность переключения крыльев.
/// </summary>
public sealed class WingFlightSystem : SharedWingFlightSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WingFlightComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<WingFlightComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<WingFlightComponent, ToggleActionEvent>(OnToggleAction);

        SubscribeLocalEvent<WingToggleComponent, MapInitEvent>(OnWingToggleMapInit);
        SubscribeLocalEvent<WingToggleComponent, ComponentShutdown>(OnWingToggleShutdown);
        SubscribeLocalEvent<WingToggleComponent, ToggleActionEvent>(OnWingToggleAction);
        SubscribeLocalEvent<WingToggleComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
    }

    private void OnComponentInit(Entity<WingFlightComponent> ent, ref ComponentInit args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action, ent.Owner);
        UpdateActionToggle(ent.Owner, ent.Comp);
        ent.Comp.CurrentScaleMultiplier = Math.Max(ent.Comp.CurrentScaleMultiplier, ent.Comp.MinScaleMultiplier);
        Dirty(ent);
    }

    private void OnComponentRemove(Entity<WingFlightComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.ActionEntity != null)
            _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);

        SetFlightEnabled(ent.Owner, false, ent.Comp);
        DisableFlightPassability(ent.Owner, ent.Comp);
        UpdateMarkings(ent.Owner, ent.Comp, enable: false);
    }

    private void OnToggleAction(Entity<WingFlightComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.ActionEntity == null || args.Action.Owner != ent.Comp.ActionEntity.Value)
            return;

        if (ent.Comp.FlightEnabled)
            args.Handled = DisableFlight(ent.Owner, ent.Comp);
        else
            args.Handled = EnableFlight(ent.Owner, ent.Comp);
    }

    private bool EnableFlight(EntityUid uid, WingFlightComponent component)
    {
        if (_standing.IsDown(uid))
            return false;

        if (!TryComp<StaminaComponent>(uid, out var stamina))
        {
            Activate(uid, component, null, 1f);
            return true;
        }

        var staminaPercent = GetStaminaPercent(stamina);
        if (staminaPercent < component.ActivationThreshold)
        {
            _popup.PopupEntity(Loc.GetString("wing-flight-popup-not-enough-stamina"), uid, uid, PopupType.Medium);
            return false;
        }

        if (!_stamina.TryTakeStamina(uid, component.ActivationStaminaDamage, stamina, visual: true))
        {
            _popup.PopupEntity(Loc.GetString("wing-flight-popup-activation-blocked"), uid, uid, PopupType.Small);
            return false;
        }

        Activate(uid, component, stamina, staminaPercent);
        return true;
    }

    private void Activate(EntityUid uid, WingFlightComponent component, StaminaComponent? stamina, float staminaPercent)
    {
        component.SustainAccumulator = 0f;
        SetFlightEnabled(uid, true, component);
        UpdateActionToggle(uid, component);
        UpdateMarkings(uid, component, enable: true);
        SetScaleImmediate(uid, component, staminaPercent);
        EnableFlightPassability(uid, component);
    }

    private bool DisableFlight(EntityUid uid, WingFlightComponent component)
    {
        SetFlightEnabled(uid, false, component);
        UpdateActionToggle(uid, component);
        UpdateMarkings(uid, component, enable: false);
        DisableFlightPassability(uid, component);
        component.SustainAccumulator = 0f;
        return true;
    }

    private void UpdateActionToggle(EntityUid uid, WingFlightComponent component)
    {
        if (component.ActionEntity == null)
            return;

        _actions.SetToggled(component.ActionEntity.Value, component.FlightEnabled);
    }

    private void UpdateMarkings(EntityUid uid, WingFlightComponent component, bool enable)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        if (!humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings) ||
            markings.Count == 0)
        {
            return;
        }

        if (enable)
            EnableMarkings(uid, component, markings, humanoid);
        else
            DisableMarkings(uid, component, markings, humanoid);
    }

    private void EnableMarkings(EntityUid uid, WingFlightComponent component, List<Marking> markings, HumanoidAppearanceComponent humanoid)
    {
        component.OriginalMarkings.Clear();

        var flightSuffix = component.Suffix;
        var openSuffix = TryComp<WingToggleComponent>(uid, out var toggle) ? toggle.Suffix : null;

        for (var i = 0; i < markings.Count; i++)
        {
            var current = markings[i].MarkingId;

            if (string.IsNullOrEmpty(current))
                continue;

            if (current.EndsWith(flightSuffix))
                continue;

            var desired = GetFlightMarkingId(current, flightSuffix, openSuffix);
            if (desired == null || !_prototype.HasIndex<MarkingPrototype>(desired))
                continue;

            component.OriginalMarkings[i] = current;
            _appearance.SetMarkingId(uid, MarkingCategories.Tail, i, desired, humanoid: humanoid);
        }

        component.AppliedMarkingOnEnable = component.OriginalMarkings.Count > 0;
    }

    private void DisableMarkings(EntityUid uid, WingFlightComponent component, List<Marking> markings, HumanoidAppearanceComponent humanoid)
    {
        if (!component.AppliedMarkingOnEnable || component.OriginalMarkings.Count == 0)
            return;

        foreach (var (index, original) in component.OriginalMarkings)
        {
            if (index < 0 || index >= markings.Count)
                continue;

            if (!_prototype.HasIndex<MarkingPrototype>(original))
                continue;

            if (markings[index].MarkingId == original)
                continue;

            _appearance.SetMarkingId(uid, MarkingCategories.Tail, index, original, humanoid: humanoid);
        }

        component.OriginalMarkings.Clear();
        component.AppliedMarkingOnEnable = false;
    }

    private static string? GetFlightMarkingId(string current, string flightSuffix, string? openSuffix)
    {
        if (!string.IsNullOrEmpty(openSuffix) && current.EndsWith(openSuffix))
        {
            var baseId = current[..^openSuffix.Length];
            return $"{baseId}{flightSuffix}";
        }

        var desired = $"{current}{flightSuffix}";
        return desired == current ? null : desired;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var entities = new List<(EntityUid uid, WingFlightComponent component)>();
        var query = EntityQueryEnumerator<ActiveWingFlightComponent, WingFlightComponent>();

        while (query.MoveNext(out var uid, out _, out var component))
        {
            entities.Add((uid, component));
        }

        foreach (var (uid, component) in entities)
        {
            TryComp(uid, out StaminaComponent? stamina);
            var staminaPercent = stamina != null ? GetStaminaPercent(stamina) : 1f;

            if (component.FlightEnabled)
            {
                if (staminaPercent <= component.AutoDisableThreshold)
                {
                    _popup.PopupEntity(Loc.GetString("wing-flight-popup-auto-disable"), uid, uid, PopupType.Medium);
                    DisableFlight(uid, component);
                    SetScaleImmediate(uid, component, staminaPercent);
                    UpdateScale(uid, component, staminaPercent, frameTime);
                    continue;
                }

                if (stamina != null)
                {
                    var staminaCost = component.SustainStaminaPerSecond * frameTime;
                    if (!_stamina.TryTakeStamina(uid, staminaCost, stamina, visual: false))
                    {
                        _popup.PopupEntity(Loc.GetString("wing-flight-popup-auto-disable"), uid, uid, PopupType.Medium);
                        DisableFlight(uid, component);
                        SetScaleImmediate(uid, component, staminaPercent);
                    }
                }
            }
            else
            {
                component.SustainAccumulator = 0f;
            }

            UpdateScale(uid, component, staminaPercent, frameTime);
        }
    }

    private void UpdateScale(EntityUid uid, WingFlightComponent component, float staminaPercent, float frameTime)
    {
        var target = component.FlightEnabled
            ? GetTargetScale(component, staminaPercent)
            : component.MinScaleMultiplier;

        var t = 1f - MathF.Exp(-component.ScaleLerpRate * frameTime);
        var newScale = MathHelper.Lerp(component.CurrentScaleMultiplier, target, t);

        if (!MathHelper.CloseTo(newScale, component.CurrentScaleMultiplier, 0.001f))
        {
            component.CurrentScaleMultiplier = newScale;
            Dirty(uid, component);
        }
    }

    private void SetScaleImmediate(EntityUid uid, WingFlightComponent component, float staminaPercent)
    {
        var target = component.FlightEnabled
            ? GetTargetScale(component, staminaPercent)
            : component.MinScaleMultiplier;

        if (!MathHelper.CloseTo(target, component.CurrentScaleMultiplier, 0.001f))
        {
            component.CurrentScaleMultiplier = target;
            Dirty(uid, component);
        }
    }

    private static float GetStaminaPercent(StaminaComponent stamina)
    {
        if (stamina.CritThreshold <= 0f)
            return 1f;

        var remaining = MathF.Max(0f, stamina.CritThreshold - stamina.StaminaDamage);
        return Math.Clamp(remaining / stamina.CritThreshold, 0f, 1f);
    }

    private void EnableFlightPassability(EntityUid uid, WingFlightComponent component)
    {
        if (!TryComp(uid, out PhysicsComponent? physics))
            return;

        EnsureComp<CanMoveInAirComponent>(uid);
        _physics.SetBodyStatus(uid, physics, BodyStatus.InAir);

        if (!TryComp(uid, out FixturesComponent? fixtures))
            return;

        // Create a copy of fixture IDs to avoid collection modification during enumeration
        var fixtureIds = new List<string>(fixtures.Fixtures.Keys);

        foreach (var id in fixtureIds)
        {
            if (!fixtures.Fixtures.TryGetValue(id, out var fixture))
                continue;

            component.OriginalCollisionMasks.TryAdd(id, fixture.CollisionMask);
            component.OriginalCollisionLayers.TryAdd(id, fixture.CollisionLayer);
            _physics.RemoveCollisionMask(uid, id, fixture, (int)CollisionGroup.MidImpassable, manager: fixtures);
        }
    }

    private void DisableFlightPassability(EntityUid uid, WingFlightComponent component)
    {
        RemCompDeferred<CanMoveInAirComponent>(uid);

        if (TryComp(uid, out PhysicsComponent? physics))
            _physics.SetBodyStatus(uid, physics, BodyStatus.OnGround);

        if (TryComp(uid, out FixturesComponent? fixtures))
        {
            // Create a copy of fixture IDs to avoid collection modification during enumeration
            var fixtureIds = new List<string>(fixtures.Fixtures.Keys);

            foreach (var id in fixtureIds)
            {
                if (!fixtures.Fixtures.TryGetValue(id, out var fixture))
                    continue;

                if (component.OriginalCollisionMasks.TryGetValue(id, out var mask))
                    _physics.SetCollisionMask(uid, id, fixture, mask, manager: fixtures);

                if (component.OriginalCollisionLayers.TryGetValue(id, out var layer))
                    _physics.SetCollisionLayer(uid, id, fixture, layer, manager: fixtures);
            }
        }

        component.OriginalCollisionMasks.Clear();
        component.OriginalCollisionLayers.Clear();
    }

    // WingToggle functionality

    private void OnWingToggleMapInit(Entity<WingToggleComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action, ent.Owner);
        UpdateWingToggleAction(ent.Owner, ent.Comp);
    }

    private void OnWingToggleShutdown(Entity<WingToggleComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ActionEntity != null)
            _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnWingToggleAction(Entity<WingToggleComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.ActionEntity == null || args.Action.Owner != ent.Comp.ActionEntity.Value)
            return;

        args.Handled = TryToggleWings(ent.Owner, ent.Comp);
    }

    public bool TryToggleWings(EntityUid uid, WingToggleComponent? component = null, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref component, ref humanoid, false))
            return false;

        if (!humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings) || markings.Count == 0)
            return false;

        if (!component.WingsOpened)
        {
            if (!CanOpenWings(uid, component))
            {
                _popup.PopupEntity(Loc.GetString("wing-toggle-open-blocked"), uid, uid, PopupType.Medium);
                return false;
            }
        }

        var openTarget = !component.WingsOpened;
        var suffix = component.Suffix;
        var changed = false;

        for (var i = 0; i < markings.Count; i++)
        {
            var current = markings[i].MarkingId;
            var desired = openTarget
                ? (current.EndsWith(suffix) ? current : $"{current}{suffix}")
                : (current.EndsWith(suffix) ? current[..^suffix.Length] : current);

            if (!_prototype.HasIndex<MarkingPrototype>(desired))
                continue;

            if (desired == current)
                continue;

            _appearance.SetMarkingId(uid, MarkingCategories.Tail, i, desired, humanoid: humanoid);
            changed = true;
        }

        if (!changed)
            return false;

        component.WingsOpened = openTarget;
        Dirty(uid, component);
        UpdateWingToggleAction(uid, component);

        if (component.WingsOpened)
        {
            EnsureComp<WingFlightComponent>(uid);
            EnsureComp<JumpAbilityComponent>(uid);
        }
        else
        {
            RemCompDeferred<WingFlightComponent>(uid);
            RemCompDeferred<JumpAbilityComponent>(uid);
        }
        return true;
    }

    private bool CanOpenWings(EntityUid uid, WingToggleComponent component)
    {
        if (component.BlockedSlots == null || component.BlockedSlots.Count == 0)
            return true;

        foreach (var slot in component.BlockedSlots)
        {
            if (_inventory.TryGetSlotEntity(uid, slot, out _))
                return false;
        }

        return true;
    }

    private void UpdateWingToggleAction(EntityUid uid, WingToggleComponent component)
    {
        if (component.ActionEntity == null)
            return;

        _actions.SetToggled(component.ActionEntity.Value, component.WingsOpened);
    }

    private void OnEquipAttempt(Entity<WingToggleComponent> ent, ref IsEquippingAttemptEvent args)
    {
        if (!ent.Comp.WingsOpened)
            return;

        if (ent.Comp.BlockedSlots != null && ent.Comp.BlockedSlots.Contains(args.Slot))
        {
            if (ent.Comp.AllowedTag != null && _tagSystem.HasTag(args.Equipment, ent.Comp.AllowedTag.Value))
                return;

            args.Cancel();
        }
    }
}

