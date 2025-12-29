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
        UpdateActionToggle(ent);
        ent.Comp.CurrentScaleMultiplier = Math.Max(ent.Comp.CurrentScaleMultiplier, ent.Comp.MinScaleMultiplier);
        Dirty(ent);
    }

    private void OnComponentRemove(Entity<WingFlightComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.ActionEntity != null)
            _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);

        SetFlightEnabled(ent, false);
        DisableFlightPassability(ent);
        UpdateMarkings(ent, enable: false);
    }

    private void OnToggleAction(Entity<WingFlightComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.ActionEntity == null || args.Action.Owner != ent.Comp.ActionEntity.Value)
            return;

        if (ent.Comp.FlightEnabled)
            args.Handled = DisableFlight(ent);
        else
            args.Handled = EnableFlight(ent);
    }

    private bool EnableFlight(Entity<WingFlightComponent> ent)
    {
        if (_standing.IsDown(ent.Owner))
            return false;

        if (!TryComp<StaminaComponent>(ent.Owner, out var stamina))
        {
            Activate(ent, 1f);
            return true;
        }

        var staminaPercent = GetStaminaPercent(stamina);
        if (staminaPercent < ent.Comp.ActivationThreshold)
        {
            _popup.PopupEntity(Loc.GetString("wing-flight-popup-not-enough-stamina"), ent.Owner, ent.Owner, PopupType.Medium);
            return false;
        }

        if (!_stamina.TryTakeStamina(ent.Owner, ent.Comp.ActivationStaminaDamage, stamina, visual: true))
        {
            _popup.PopupEntity(Loc.GetString("wing-flight-popup-activation-blocked"), ent.Owner, ent.Owner, PopupType.Small);
            return false;
        }

        Activate(ent, staminaPercent);
        return true;
    }

    private void Activate(Entity<WingFlightComponent> ent, float staminaPercent)
    {
        ent.Comp.SustainAccumulator = 0f;
        SetFlightEnabled(ent, true);
        UpdateActionToggle(ent);
        UpdateMarkings(ent, enable: true);
        SetScaleImmediate(ent, staminaPercent);
        EnableFlightPassability(ent);
    }

    private bool DisableFlight(Entity<WingFlightComponent> ent)
    {
        SetFlightEnabled(ent, false);
        UpdateActionToggle(ent);
        UpdateMarkings(ent, enable: false);
        DisableFlightPassability(ent);
        ent.Comp.SustainAccumulator = 0f;
        return true;
    }

    private void UpdateActionToggle(Entity<WingFlightComponent> ent)
    {
        if (ent.Comp.ActionEntity == null)
            return;

        _actions.SetToggled(ent.Comp.ActionEntity.Value, ent.Comp.FlightEnabled);
    }

    private void UpdateMarkings(Entity<WingFlightComponent> ent, bool enable)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humanoid))
            return;

        if (!humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings) ||
            markings.Count == 0)
        {
            return;
        }

        if (enable)
            EnableMarkings(ent, markings, humanoid);
        else
            DisableMarkings(ent, markings, humanoid);
    }

    private void EnableMarkings(Entity<WingFlightComponent> ent, List<Marking> markings, HumanoidAppearanceComponent humanoid)
    {
        ent.Comp.OriginalMarkings.Clear();

        var flightSuffix = ent.Comp.Suffix;
        var openSuffix = TryComp<WingToggleComponent>(ent.Owner, out var toggle) ? toggle.Suffix : null;

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

            ent.Comp.OriginalMarkings[i] = current;
            _appearance.SetMarkingId(ent.Owner, MarkingCategories.Tail, i, desired, humanoid: humanoid);
        }

        ent.Comp.AppliedMarkingOnEnable = ent.Comp.OriginalMarkings.Count > 0;
    }

    private void DisableMarkings(Entity<WingFlightComponent> ent, List<Marking> markings, HumanoidAppearanceComponent humanoid)
    {
        if (!ent.Comp.AppliedMarkingOnEnable || ent.Comp.OriginalMarkings.Count == 0)
            return;

                        foreach (var (index, original) in ent.Comp.OriginalMarkings)
        {
            if (index < 0 || index >= markings.Count)
                continue;

            if (!_prototype.HasIndex<MarkingPrototype>(original))
                continue;

            if (markings[index].MarkingId == original)
                continue;

            _appearance.SetMarkingId(ent.Owner, MarkingCategories.Tail, index, original, humanoid: humanoid);
        }

        ent.Comp.OriginalMarkings.Clear();
        ent.Comp.AppliedMarkingOnEnable = false;
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

        var entities = new List<Entity<WingFlightComponent>>();
        var query = EntityQueryEnumerator<ActiveWingFlightComponent, WingFlightComponent>();

        while (query.MoveNext(out var uid, out _, out var flightComp))
        {
            entities.Add((uid, flightComp));
        }

        foreach (var ent in entities)
        {
            TryComp(ent.Owner, out StaminaComponent? stamina);
            var staminaPercent = stamina != null ? GetStaminaPercent(stamina) : 1f;

            if (ent.Comp.FlightEnabled)
            {
                if (staminaPercent <= ent.Comp.AutoDisableThreshold)
                {
                    _popup.PopupEntity(Loc.GetString("wing-flight-popup-auto-disable"), ent.Owner, ent.Owner, PopupType.Medium);
                    DisableFlight(ent);
                    SetScaleImmediate(ent, staminaPercent);
                    UpdateScale(ent, staminaPercent, frameTime);
                    continue;
                }

                if (stamina != null)
                {
                    var staminaCost = ent.Comp.SustainStaminaPerSecond * frameTime;
                    if (!_stamina.TryTakeStamina(ent.Owner, staminaCost, stamina, visual: false))
                    {
                        _popup.PopupEntity(Loc.GetString("wing-flight-popup-auto-disable"), ent.Owner, ent.Owner, PopupType.Medium);
                        DisableFlight(ent);
                        SetScaleImmediate(ent, staminaPercent);
                    }
                }
            }
            else
            {
                ent.Comp.SustainAccumulator = 0f;
            }

            UpdateScale(ent, staminaPercent, frameTime);
        }
    }

    private void UpdateScale(Entity<WingFlightComponent> ent, float staminaPercent, float frameTime)
    {
        var target = ent.Comp.FlightEnabled
            ? GetTargetScale(ent, staminaPercent)
            : ent.Comp.MinScaleMultiplier;

        var t = 1f - MathF.Exp(-ent.Comp.ScaleLerpRate * frameTime);
        var newScale = MathHelper.Lerp(ent.Comp.CurrentScaleMultiplier, target, t);

        if (!MathHelper.CloseTo(newScale, ent.Comp.CurrentScaleMultiplier, 0.001f))
        {
            ent.Comp.CurrentScaleMultiplier = newScale;
            Dirty(ent);
        }
    }

    private void SetScaleImmediate(Entity<WingFlightComponent> ent, float staminaPercent)
    {
        var target = ent.Comp.FlightEnabled
            ? GetTargetScale(ent, staminaPercent)
            : ent.Comp.MinScaleMultiplier;

        if (!MathHelper.CloseTo(target, ent.Comp.CurrentScaleMultiplier, 0.001f))
        {
            ent.Comp.CurrentScaleMultiplier = target;
            Dirty(ent);
        }
    }

    private static float GetStaminaPercent(StaminaComponent stamina)
    {
        if (stamina.CritThreshold <= 0f)
            return 1f;

        var remaining = MathF.Max(0f, stamina.CritThreshold - stamina.StaminaDamage);
        return Math.Clamp(remaining / stamina.CritThreshold, 0f, 1f);
    }

    private void EnableFlightPassability(Entity<WingFlightComponent> ent)
    {
        if (!TryComp(ent.Owner, out PhysicsComponent? physics))
            return;

        EnsureComp<CanMoveInAirComponent>(ent.Owner);
        _physics.SetBodyStatus(ent.Owner, physics, BodyStatus.InAir);

        if (!TryComp(ent.Owner, out FixturesComponent? fixtures))
            return;

        // Создаём копию идентификаторов фикстур, чтобы избежать изменения коллекции во время перечисления
        var fixtureIds = new List<string>(fixtures.Fixtures.Keys);

        foreach (var id in fixtureIds)
        {
            if (!fixtures.Fixtures.TryGetValue(id, out var fixture))
                continue;

            ent.Comp.OriginalCollisionMasks.TryAdd(id, fixture.CollisionMask);
            ent.Comp.OriginalCollisionLayers.TryAdd(id, fixture.CollisionLayer);
            _physics.RemoveCollisionMask(ent.Owner, id, fixture, (int)CollisionGroup.MidImpassable, manager: fixtures);
        }
    }

    private void DisableFlightPassability(Entity<WingFlightComponent> ent)
    {
        RemCompDeferred<CanMoveInAirComponent>(ent.Owner);

        if (TryComp(ent.Owner, out PhysicsComponent? physics))
            _physics.SetBodyStatus(ent.Owner, physics, BodyStatus.OnGround);

        if (TryComp(ent.Owner, out FixturesComponent? fixtures))
        {
            // Создаём копию идентификаторов фикстур, чтобы избежать изменения коллекции во время перечисления
            var fixtureIds = new List<string>(fixtures.Fixtures.Keys);

            foreach (var id in fixtureIds)
            {
                if (!fixtures.Fixtures.TryGetValue(id, out var fixture))
                    continue;

                if (ent.Comp.OriginalCollisionMasks.TryGetValue(id, out var mask))
                    _physics.SetCollisionMask(ent.Owner, id, fixture, mask, manager: fixtures);

                if (ent.Comp.OriginalCollisionLayers.TryGetValue(id, out var layer))
                    _physics.SetCollisionLayer(ent.Owner, id, fixture, layer, manager: fixtures);
            }
        }

        ent.Comp.OriginalCollisionMasks.Clear();
        ent.Comp.OriginalCollisionLayers.Clear();
    }

    // WingToggle функционал

    private void OnWingToggleMapInit(Entity<WingToggleComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action, ent.Owner);
        UpdateWingToggleAction(ent);
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

        args.Handled = TryToggleWings(ent);
    }

    public bool TryToggleWings(Entity<WingToggleComponent> ent, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(ent.Owner, ref humanoid, false))
            return false;

        if (!humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings) || markings.Count == 0)
            return false;

        if (!ent.Comp.WingsOpened)
        {
            if (!CanOpenWings(ent))
            {
                _popup.PopupEntity(Loc.GetString("wing-toggle-open-blocked"), ent.Owner, ent.Owner, PopupType.Medium);
                return false;
            }
        }

        var openTarget = !ent.Comp.WingsOpened;
        var suffix = ent.Comp.Suffix;
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

            _appearance.SetMarkingId(ent.Owner, MarkingCategories.Tail, i, desired, humanoid: humanoid);
            changed = true;
        }

        if (!changed)
            return false;

        ent.Comp.WingsOpened = openTarget;
        Dirty(ent);
        UpdateWingToggleAction(ent);

        if (ent.Comp.WingsOpened)
        {
            EnsureComp<WingFlightComponent>(ent.Owner);
            EnsureComp<JumpAbilityComponent>(ent.Owner);
        }
        else
        {
            RemCompDeferred<WingFlightComponent>(ent.Owner);
            RemCompDeferred<JumpAbilityComponent>(ent.Owner);
        }
        return true;
    }

    private bool CanOpenWings(Entity<WingToggleComponent> ent)
    {
        if (ent.Comp.BlockedSlots == null || ent.Comp.BlockedSlots.Count == 0)
            return true;

        foreach (var slot in ent.Comp.BlockedSlots)
        {
            if (_inventory.TryGetSlotEntity(ent.Owner, slot, out _))
                return false;
        }

        return true;
    }

    private void UpdateWingToggleAction(Entity<WingToggleComponent> ent)
    {
        if (ent.Comp.ActionEntity == null)
            return;

        _actions.SetToggled(ent.Comp.ActionEntity.Value, ent.Comp.WingsOpened);
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

