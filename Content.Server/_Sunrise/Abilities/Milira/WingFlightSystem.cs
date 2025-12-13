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
using Content.Shared._Sunrise.Abilities.Milira;
using Content.Shared.Physics;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Sunrise.Abilities.Milira;

/// <summary>
/// Серверная система для полёта расы милира, оно использует другую систему для изменения масштаба крыльев, а также изменяет маркинг, и тратит стамину.
/// </summary>
public sealed class WingFlightSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedWingFlightSystem _shared = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WingFlightComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<WingFlightComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<WingFlightComponent, ToggleActionEvent>(OnToggleAction);
    }

    private void OnComponentInit(EntityUid uid, WingFlightComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.Action, uid);
        UpdateActionToggle(uid, component);
        component.CurrentScaleMultiplier = Math.Max(component.CurrentScaleMultiplier, component.MinScaleMultiplier);
        Dirty(uid, component);
    }

    private void OnComponentRemove(EntityUid uid, WingFlightComponent component, ComponentRemove args)
    {
        if (component.ActionEntity != null)
            _actions.RemoveAction(uid, component.ActionEntity);

        _shared.SetFlightEnabled(uid, false, component);
        DisableFlightPassability(uid, component);
        UpdateMarkings(uid, component, enable: false);
        component.SustainAccumulator = 0f;
        component.AppliedMarkingOnEnable = false;
        component.OriginalMarkings.Clear();
    }

    private void OnToggleAction(EntityUid uid, WingFlightComponent component, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.ActionEntity == null || args.Action.Owner != component.ActionEntity.Value)
            return;

        args.Handled = true;

        if (component.FlightEnabled)
            DisableFlight(uid, component);
        else
            EnableFlight(uid, component);
    }

    private void EnableFlight(EntityUid uid, WingFlightComponent component)
    {
        if (_standing.IsDown(uid))
            return;

        if (!TryComp(uid, out StaminaComponent? stamina))
        {
            Activate(uid, component, null, 1f);
            return;
        }

        var staminaPercent = GetStaminaPercent(stamina);
        if (staminaPercent < component.ActivationThreshold)
        {
            _popup.PopupEntity(Loc.GetString("wing-flight-popup-not-enough-stamina"), uid, uid, PopupType.Medium);
            return;
        }

        if (!_stamina.TryTakeStamina(uid, component.ActivationStaminaDamage, stamina, visual: true))
        {
            _popup.PopupEntity(Loc.GetString("wing-flight-popup-activation-blocked"), uid, uid, PopupType.Small);
            return;
        }

        Activate(uid, component, stamina, staminaPercent);
    }

    private void Activate(EntityUid uid, WingFlightComponent component, StaminaComponent? stamina, float staminaPercent)
    {
        component.SustainAccumulator = 0f;
        _shared.SetFlightEnabled(uid, true, component);
        UpdateActionToggle(uid, component);
        UpdateMarkings(uid, component, enable: true);
        SetScaleImmediate(uid, component, staminaPercent);
        EnableFlightPassability(uid, component);
    }

    private void DisableFlight(EntityUid uid, WingFlightComponent component)
    {
        _shared.SetFlightEnabled(uid, false, component);
        UpdateActionToggle(uid, component);
        UpdateMarkings(uid, component, enable: false);
        DisableFlightPassability(uid, component);
        component.SustainAccumulator = 0f;
    }

    private void UpdateActionToggle(EntityUid uid, WingFlightComponent component)
    {
        if (component.ActionEntity == null)
            return;

        _actions.SetToggled(component.ActionEntity.Value, component.FlightEnabled);
    }

    private void UpdateMarkings(EntityUid uid, WingFlightComponent component, bool enable)
    {
        if (!TryComp(uid, out HumanoidAppearanceComponent? humanoid))
            return;

        if (!humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings) ||
            markings.Count == 0)
        {
            return;
        }

        var flightSuffix = component.Suffix;
        var changed = false;
        var openSuffix = TryComp<WingToggleComponent>(uid, out var toggle) ? toggle.Suffix : null;

        if (enable)
        {
            component.OriginalMarkings.Clear();

            for (var i = 0; i < markings.Count; i++)
            {
                var current = markings[i].MarkingId;

                if (current.EndsWith(flightSuffix))
                    continue;

                string desired;
                if (!string.IsNullOrEmpty(openSuffix) && current.EndsWith(openSuffix))
                {
                    var baseId = current[..^openSuffix.Length];
                    desired = $"{baseId}{flightSuffix}";
                    if (!_prototype.HasIndex<MarkingPrototype>(desired))
                        continue;
                }
                else
                {
                    desired = $"{current}{flightSuffix}";
                    if (desired == current || !_prototype.HasIndex<MarkingPrototype>(desired))
                        continue;
                }

                component.OriginalMarkings[i] = current;
                _appearance.SetMarkingId(uid, MarkingCategories.Tail, i, desired, humanoid: humanoid);
                changed = true;
            }

            component.AppliedMarkingOnEnable = component.OriginalMarkings.Count > 0;
        }
        else
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
                changed = true;
            }

            component.OriginalMarkings.Clear();
            component.AppliedMarkingOnEnable = false;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<WingFlightComponent>();

        while (query.MoveNext(out var uid, out var component))
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
                    continue;
                }

                component.SustainAccumulator += frameTime;

                while (component.SustainAccumulator >= 1f)
                {
                    component.SustainAccumulator -= 1f;

                    if (stamina == null)
                        continue;

                    if (_stamina.TryTakeStamina(uid, component.SustainStaminaPerSecond, stamina, visual: false))
                        continue;

                    _popup.PopupEntity(Loc.GetString("wing-flight-popup-auto-disable"), uid, uid, PopupType.Medium);
                    DisableFlight(uid, component);
                    SetScaleImmediate(uid, component, staminaPercent);
                    break;
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
            ? _shared.GetTargetScale(component, staminaPercent)
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
            ? _shared.GetTargetScale(component, staminaPercent)
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

        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            component.OriginalCollisionMasks.TryAdd(id, fixture.CollisionMask);
            component.OriginalCollisionLayers.TryAdd(id, fixture.CollisionLayer);
            _physics.RemoveCollisionMask(uid, id, fixture, (int) CollisionGroup.MidImpassable, manager: fixtures);
        }
    }

    private void DisableFlightPassability(EntityUid uid, WingFlightComponent component)
    {
        RemCompDeferred<CanMoveInAirComponent>(uid);

        if (TryComp(uid, out PhysicsComponent? physics))
            _physics.SetBodyStatus(uid, physics, BodyStatus.OnGround);

        if (TryComp(uid, out FixturesComponent? fixtures))
        {
            foreach (var (id, fixture) in fixtures.Fixtures)
            {
                if (component.OriginalCollisionMasks.TryGetValue(id, out var mask))
                    _physics.SetCollisionMask(uid, id, fixture, mask, manager: fixtures);

                if (component.OriginalCollisionLayers.TryGetValue(id, out var layer))
                    _physics.SetCollisionLayer(uid, id, fixture, layer, manager: fixtures);
            }
        }

        component.OriginalCollisionMasks.Clear();
        component.OriginalCollisionLayers.Clear();
    }
}

