using System.Diagnostics.CodeAnalysis;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed class SatiationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;

    private (string, StatusIconPrototype?)[] HungerIcons = new (string, StatusIconPrototype?)[] {
        ("HungerIconOverfed", null),
        ("HungerIconPeckish", null),
        ("HungerIconStarving", null)
    };
    /// <summary>
    /// A dictionary relating hunger thresholds to corresponding alerts.
    /// </summary>
    private Dictionary<SatiationThreashold, AlertType> HungerAlertThresholds = new()
    {
        { SatiationThreashold.Concerned, AlertType.Peckish },
        { SatiationThreashold.Desperate, AlertType.Starving },
        { SatiationThreashold.Dead, AlertType.Starving }
    };
    private AlertCategory HungerAlertCategory = AlertCategory.Hunger;

    private (string, StatusIconPrototype?)[] ThirstIcons = new (string, StatusIconPrototype?)[] {
        ("ThirstIconOverhydrated", null),
        ("ThirstIconThirsty", null),
        ("ThirstIconParched", null)
    };
    /// <summary>
    /// A dictionary relating hunger thresholds to corresponding alerts.
    /// </summary>
    private Dictionary<SatiationThreashold, AlertType> ThirstAlertThresholds = new()
    {
        { SatiationThreashold.Concerned, AlertType.Thirsty },
        { SatiationThreashold.Desperate, AlertType.Parched },
        { SatiationThreashold.Dead, AlertType.Parched }
    };
    private AlertCategory ThirstAlertCategory = AlertCategory.Thirst;

    public override void Initialize()
    {
        base.Initialize();

        foreach (var pair in ThirstIcons)
        {
            var (iconId, prototype) = pair;
            DebugTools.Assert(_prototype.TryIndex(iconId, out prototype));
        }
        foreach (var pair in HungerIcons)
        {
            var (iconId, prototype) = pair;
            DebugTools.Assert(_prototype.TryIndex(iconId, out prototype));
        }

        SubscribeLocalEvent<SatiationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SatiationComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SatiationComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<SatiationComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void InitializeSatiation(EntityUid uid, Satiation component, Dictionary<SatiationThreashold, AlertType> alertThresholds, AlertCategory alertCategory, MapInitEvent args)
    {
        var amount = _random.Next(
            (int) component.Thresholds[SatiationThreashold.Concerned] + 10,
            (int) component.Thresholds[SatiationThreashold.Okay]);
        SetSatiation(component, amount);
        UpdateCurrentThreshold(component);
        DoThresholdEffects(uid, component, alertThresholds, alertCategory, false);

        component.CurrentThreshold = GetThreshold(component, component.Current);
        component.LastThreshold = SatiationThreashold.Okay; // TODO: Potentially change this -> Used Okay because no effects.
    }

    private void OnMapInit(EntityUid uid, SatiationComponent component, MapInitEvent args)
    {
        InitializeSatiation(uid, component.Thirst, ThirstAlertThresholds, ThirstAlertCategory, args);
        InitializeSatiation(uid, component.Hunger, HungerAlertThresholds, HungerAlertCategory, args);
        Dirty(uid, component);

        if (TryComp(uid, out MovementSpeedModifierComponent? moveMod))
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid, moveMod);
    }

    private void OnShutdown(EntityUid uid, SatiationComponent component, ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(uid, HungerAlertCategory);
        _alerts.ClearAlertCategory(uid, ThirstAlertCategory);
    }

    private void OnRefreshMovespeed(EntityUid uid, SatiationComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (_jetpack.IsUserFlying(uid))
            return;

        if (component.Thirst.CurrentThreshold <= SatiationThreashold.Desperate)
            args.ModifySpeed(component.Thirst.SlowdownModifier, component.Thirst.SlowdownModifier);
        if (component.Hunger.CurrentThreshold <= SatiationThreashold.Desperate)
            args.ModifySpeed(component.Hunger.SlowdownModifier, component.Hunger.SlowdownModifier);
    }

    private void OnRejuvenate(EntityUid uid, SatiationComponent component, RejuvenateEvent args)
    {
        SetThirst((uid, component), component.Thirst.Thresholds[SatiationThreashold.Okay]);
        SetHunger((uid, component), component.Hunger.Thresholds[SatiationThreashold.Okay]);
    }

    /// <summary>
    /// Adds to the current thirst of an entity by the specified value
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="amount"></param>
    /// <param name="component"></param>
    public void ModifyThirst(Entity<SatiationComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;
        SetThirst(ent, ent.Comp.Thirst.Current + amount);
    }

    /// <summary>
    /// Adds to the current hunger of an entity by the specified value
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="amount"></param>
    /// <param name="component"></param>
    public void ModifyHunger(Entity<SatiationComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;
        SetHunger(ent, ent.Comp.Hunger.Current + amount);
    }

    private void SetSatiation(Satiation satiation, float amount)
    {
        satiation.Current = Math.Clamp(amount,
            satiation.Thresholds[SatiationThreashold.Dead],
            satiation.Thresholds[SatiationThreashold.Full]);
    }

    /// <summary>
    /// Sets the current thirst of an entity to the specified value
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="amount"></param>
    /// <param name="component"></param>
    public void SetThirst(Entity<SatiationComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;
        SetSatiation(ent.Comp.Thirst, amount);
        UpdateCurrentThirstThreshold(ent);
        Dirty(ent);
    }

    /// <summary>
    /// Sets the current hunger of an entity to the specified value
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="amount"></param>
    /// <param name="component"></param>
    public void SetHunger(Entity<SatiationComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;
        SetSatiation(ent.Comp.Hunger, amount);
        UpdateCurrentHungerThreshold(ent);
        Dirty(ent);
    }

    private void UpdateCurrentThreshold(Satiation satiation)
    {
        var calculatedNutritionThreshold = GetThreshold(satiation);
        if (calculatedNutritionThreshold == satiation.CurrentThreshold)
            return;
        satiation.CurrentThreshold = calculatedNutritionThreshold;
        if (satiation.ThresholdDamage.TryGetValue(satiation.CurrentThreshold, out var damage))
            satiation.CurrentThresholdDamage = damage;
        else
            satiation.CurrentThresholdDamage = null;
    }

    private void UpdateCurrentThirstThreshold(Entity<SatiationComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;
        UpdateCurrentThreshold(ent.Comp.Thirst);
        DoThirstThresholdEffects((ent.Owner, ent.Comp));
        Dirty(ent);
    }

    private void UpdateCurrentHungerThreshold(Entity<SatiationComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;
        UpdateCurrentThreshold(ent.Comp.Hunger);
        DoHungerThresholdEffects((ent.Owner, ent.Comp));

        Dirty(ent);
    }

    private bool DoThresholdEffects(EntityUid uid, Satiation satiation, Dictionary<SatiationThreashold, AlertType> alertThresholds, AlertCategory alertCategory, bool force)
    {
        if (satiation.CurrentThreshold == satiation.LastThreshold && !force)
            return false;

        if (GetMovementThreshold(satiation.CurrentThreshold) != GetMovementThreshold(satiation.LastThreshold))
        {
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        }
        if (satiation.ThresholdDecayModifiers.TryGetValue(satiation.CurrentThreshold, out var modifier))
        {
            satiation.ActualDecayRate = satiation.BaseDecayRate * modifier;
        }
        satiation.LastThreshold = satiation.CurrentThreshold;

        if (alertThresholds.TryGetValue(satiation.CurrentThreshold, out var alertId))
        {
            _alerts.ShowAlert(uid, alertId);
        }
        else
        {
            _alerts.ClearAlertCategory(uid, alertCategory);
        }

        return true;
    }

    private void DoThirstThresholdEffects(Entity<SatiationComponent> ent, bool force = false)
    {
        if (!DoThresholdEffects(ent.Owner, ent.Comp.Thirst, ThirstAlertThresholds, ThirstAlertCategory, force))
            return;
    }

    private void DoHungerThresholdEffects(Entity<SatiationComponent> ent, bool force = false)
    {
        if (!DoThresholdEffects(ent.Owner, ent.Comp.Hunger, HungerAlertThresholds, HungerAlertCategory, force))
            return;
    }

    private void DoContinuousEffects(EntityUid uid, Satiation satiation)
    {
        if (!_mobState.IsDead(uid) &&
            satiation.CurrentThresholdDamage is { } damage)
        {
            _damageable.TryChangeDamage(uid, damage, true, false);
        }
    }

    private SatiationThreashold GetThreshold(Satiation satiation, float? level = null)
    {
        level ??= satiation.Current;
        var result = SatiationThreashold.Dead;
        var value = satiation.Thresholds[SatiationThreashold.Full];
        foreach (var threshold in satiation.Thresholds)
        {
            if (threshold.Value <= value && threshold.Value >= level)
            {
                result = threshold.Key;
                value = threshold.Value;
            }
        }
        return result;
    }

    /// <summary>
    /// Gets the thirst threshold for an entity based on the amount of thirst specified.
    /// If a specific amount isn't specified, just uses the current thirst of the entity
    /// </summary>
    /// <param name="component"></param>
    /// <param name="thirst"></param>
    /// <returns></returns>
    public SatiationThreashold GetThirstThreshold(SatiationComponent component, float? thirst = null)
    {
        return GetThreshold(component.Thirst, thirst);
    }

    /// <summary>
    /// Gets the hunger threshold for an entity based on the amount of food specified.
    /// If a specific amount isn't specified, just uses the current hunger of the entity
    /// </summary>
    /// <param name="component"></param>
    /// <param name="food"></param>
    /// <returns></returns>
    public SatiationThreashold GetHungerThreshold(SatiationComponent component, float? food = null)
    {
        return GetThreshold(component.Hunger, food);
    }

    /// <summary>
    /// A check that returns if the entity is below a thirst threshold.
    /// </summary>
    public bool IsThirstBelowState(Entity<SatiationComponent?> ent, SatiationThreashold threshold, float? thirst = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false; // It's never going to go thirsty, so it's probably fine to assume that it's not... you know, thirsty.

        return GetThirstThreshold(ent.Comp, thirst) < threshold;
    }

    /// <summary>
    /// A check that returns if the entity is below a hunger threshold.
    /// </summary>
    public bool IsHungerBelowState(Entity<SatiationComponent?> ent, SatiationThreashold threshold, float? food = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false; // It's never going to go hungry, so it's probably fine to assume that it's not... you know, hungry.

        return GetHungerThreshold(ent.Comp, food) < threshold;
    }

    private bool GetMovementThreshold(SatiationThreashold threshold)
    {
        switch (threshold)
        {
            case SatiationThreashold.Full:
            case SatiationThreashold.Okay:
                return true;
            case SatiationThreashold.Concerned:
            case SatiationThreashold.Desperate:
            case SatiationThreashold.Dead:
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(threshold), threshold, null);
        }
    }

    private bool TryGetStatusIconPrototype(Satiation satiation, (string, StatusIconPrototype?)[] Icons, [NotNullWhen(true)] out StatusIconPrototype? prototype)
    {
        switch (satiation.CurrentThreshold)
        {
            case SatiationThreashold.Full:
                prototype = Icons?[0].Item2;
                break;
            case SatiationThreashold.Concerned:
                prototype = Icons?[1].Item2;
                break;
            case SatiationThreashold.Desperate:
                prototype = Icons?[2].Item2;
                break;
            default:
                prototype = null;
                break;
        }

        return prototype != null;
    }

    public bool TryGetStatusHungerIconPrototype(SatiationComponent component, [NotNullWhen(true)] out StatusIconPrototype? prototype)
    {
        return TryGetStatusIconPrototype(component.Hunger, HungerIcons, out prototype);
    }

    public bool TryGetStatusThirstIconPrototype(SatiationComponent component, [NotNullWhen(true)] out StatusIconPrototype? prototype)
    {
        return TryGetStatusIconPrototype(component.Thirst, ThirstIcons, out prototype);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SatiationComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime < component.NextUpdateTime)
                continue;
            component.NextUpdateTime = _timing.CurTime + component.UpdateRate;

            ModifyThirst((uid, component), -component.Thirst.ActualDecayRate);
            DoContinuousEffects(uid, component.Thirst);
            ModifyHunger((uid, component), -component.Hunger.ActualDecayRate);
            DoContinuousEffects(uid, component.Hunger);
        }
    }
}

