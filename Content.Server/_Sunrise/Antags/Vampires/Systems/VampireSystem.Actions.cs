using Content.Server.Charges;
using Content.Server._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires;
using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared.Actions.Components;
using Content.Shared.Charges.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Antags.Vampires.Systems;

public sealed partial class VampireSystem
{
    // Выдача и настройка действий.

    [Dependency] private readonly IComponentFactory _componentFactory = null!;
    [Dependency] private readonly ChargesSystem _charges = null!;

    private readonly List<EntProtoId> _missingActions = [];

    private void InitializeActions()
    {
        SubscribeLocalEvent<ActionsComponent, ComponentStartup>(OnActionsComponentStartup);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnActionsComponentStartup(Entity<ActionsComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<VampireComponent>(ent, out var vampire))
            return;

        SyncVampireActions((ent.Owner, vampire));
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!TryComp<VampireComponent>(ev.Entity, out var vampire))
            return;

        SyncVampireActions((ev.Entity, vampire));
    }

    private void SyncVampireActions(Entity<VampireComponent> ent)
    {
        if (!TryComp<VampireActionStateComponent>(ent, out var actionState) ||
            !TryComp<VampireConfigurationComponent>(ent, out var configuration))
        {
            return;
        }

        CleanMissingActions(actionState);
        EnsureRejuvenateUpgrade(ent, configuration);
        RefreshAllActions(ent, configuration);
    }

    private void CleanMissingActions(VampireActionStateComponent actionState)
    {
        if (actionState.Actions.Count == 0)
            return;

        _missingActions.Clear();
        foreach (var (actionId, actionEntity) in actionState.Actions)
        {
            if (!Exists(actionEntity))
                _missingActions.Add(actionId);
        }

        foreach (var actionId in _missingActions)
        {
            actionState.Actions.Remove(actionId);
        }
    }

    private void EnsureRejuvenateUpgrade(
        Entity<VampireComponent> ent,
        VampireConfigurationComponent configuration)
    {
        if (!TryComp<VampireActionStateComponent>(ent, out var actionState))
            return;

        var upgradedActionId = configuration.RejuvenateUpgradedAction;
        var baseActionId = configuration.RejuvenateAction;
        var requiredPowerLevel = GetRequiredPowerLevel(upgradedActionId);
        if (ent.Comp.PowerLevel < requiredPowerLevel)
            return;

        VampireActionChargeState? previousChargeState = null;
        if (actionState.Actions.TryGetValue(baseActionId, out var firstAction))
            previousChargeState = CaptureActionChargeState(firstAction);

        if (!actionState.Actions.ContainsKey(upgradedActionId))
        {
            EntityUid? action = null;
            _actions.AddAction(ent.Owner, ref action, upgradedActionId, ent.Owner);
            if (action is { } actionEntity)
            {
                actionState.Actions[upgradedActionId] = actionEntity;
                ConfigureVampireAction(ent, configuration, upgradedActionId, actionEntity, previousChargeState);
            }
        }

        if (actionState.Actions.TryGetValue(upgradedActionId, out var secondAction))
            TryRefreshVampireAction(ent, configuration, upgradedActionId, secondAction);

        if (!actionState.Actions.TryGetValue(baseActionId, out firstAction))
            return;

        _actions.RemoveAction(ent.Owner, firstAction);
        actionState.Actions.Remove(baseActionId);
    }

    private void RefreshAllActions(
        Entity<VampireComponent> ent,
        VampireConfigurationComponent configuration)
    {
        if (!TryComp<VampireActionStateComponent>(ent, out var actionState))
            return;

        foreach (var (actionId, actionEntity) in actionState.Actions)
        {
            TryRefreshVampireAction(ent, configuration, actionId, actionEntity);
        }
    }

    private void GrantBaseActions(
        Entity<VampireComponent> ent,
        VampireConfigurationComponent configuration)
    {
        var actionState = EnsureComp<VampireActionStateComponent>(ent);
        foreach (var actionId in configuration.BaseActions)
        {
            EntityUid? action = null;
            _actions.AddAction(ent.Owner, ref action, actionId, ent.Owner);

            if (action is { } actionEntity)
                actionState.Actions[actionId] = actionEntity;
        }
    }

    private void RemoveVampireActions(Entity<VampireComponent> ent)
    {
        if (!TryComp<VampireActionStateComponent>(ent, out var actionState))
            return;

        foreach (var action in actionState.Actions.Values)
        {
            _actions.RemoveAction(ent.Owner, action);
        }

        actionState.Actions.Clear();
    }

    private void TryRefreshVampireAction(
        Entity<VampireComponent> ent,
        VampireConfigurationComponent configuration,
        EntProtoId actionId,
        EntityUid? actionEntity)
    {
        if (actionEntity is null || _actions.GetAction(actionEntity) is not { } action)
            return;

        if (!TryComp<VampireActionComponent>(actionEntity.Value, out var vampireAction))
        {
            _actions.SetEnabled(action.AsNullable(), true);
            return;
        }

        ConfigureVampireAction(ent, configuration, actionId, actionEntity.Value);
        _actions.SetEnabled(action.AsNullable(), ent.Comp.PowerLevel >= vampireAction.RequiredPowerLevel);
    }

    private void ConfigureVampireAction(
        Entity<VampireComponent> ent,
        VampireConfigurationComponent configuration,
        EntProtoId actionId,
        EntityUid actionEntity,
        VampireActionChargeState? previousChargeState = null)
    {
        if (!TryGetPowerLevelPrototype(ent.Comp.PowerLevel, out var level))
            return;

        VampireActionChargeSettings? chargeSettings = null;

        if (actionId == configuration.FangsAction)
        {
            _actions.SetUseDelay(actionEntity, configuration.FangsUseDelay);
        }
        else if (actionId == configuration.GlareAction)
        {
            chargeSettings = level.Glare.Action;
        }
        else if (actionId == configuration.SleepAction)
        {
            chargeSettings = level.Sleep.Action;

            if (TryComp<VampireActionComponent>(actionEntity, out var vampireAction))
                vampireAction.BloodCost = level.Sleep.BloodCost;

            _actions.SetRange(actionEntity, level.Sleep.TargetRange);
        }
        else if (actionId == configuration.RejuvenateAction ||
                 actionId == configuration.RejuvenateUpgradedAction)
        {
            chargeSettings = level.Rejuvenation.Action;
        }

        if (chargeSettings is null)
            return;

        _actions.SetUseDelay(actionEntity, chargeSettings.UseDelay);
        ConfigureActionCharges(actionEntity, chargeSettings, previousChargeState);
    }

    private void ConfigureActionCharges(
        EntityUid actionEntity,
        VampireActionChargeSettings settings,
        VampireActionChargeState? previousChargeState = null)
    {
        if (!TryComp<LimitedChargesComponent>(actionEntity, out var charges) ||
            !TryComp<AutoRechargeComponent>(actionEntity, out var recharge))
        {
            return;
        }

        var previous = previousChargeState ?? CaptureActionChargeState(actionEntity);
        var addedCapacity = Math.Max(0, settings.MaxCharges - previous.MaxCharges);
        var newCharges = Math.Clamp(previous.CurrentCharges + addedCapacity, 0, settings.MaxCharges);

        _charges.SetMaxCharges((actionEntity, charges), settings.MaxCharges);
        _charges.SetCharges((actionEntity, charges), newCharges);
        _charges.SetRechargeDuration(
            (actionEntity, charges, recharge),
            settings.RechargeDuration,
            newCharges < settings.MaxCharges ? previous.RechargeProgress : 0f);
    }

    private VampireActionChargeState CaptureActionChargeState(EntityUid actionEntity)
    {
        if (!TryComp<LimitedChargesComponent>(actionEntity, out var charges) ||
            !TryComp<AutoRechargeComponent>(actionEntity, out var recharge))
        {
            return new VampireActionChargeState(0, 0, 0f);
        }

        var currentCharges = _charges.GetCurrentCharges((actionEntity, charges, recharge));
        var progress = 0f;

        if (currentCharges >= charges.MaxCharges || recharge.RechargeDuration <= TimeSpan.Zero)
            return new VampireActionChargeState(currentCharges, charges.MaxCharges, progress);

        var elapsed = _timing.CurTime - charges.LastUpdate;
        var elapsedTicks = Math.Max(0L, elapsed.Ticks % recharge.RechargeDuration.Ticks);
        progress = Math.Clamp((float)elapsedTicks / recharge.RechargeDuration.Ticks, 0f, 1f);

        return new VampireActionChargeState(currentCharges, charges.MaxCharges, progress);
    }

    private VampirePowerLevel GetRequiredPowerLevel(EntProtoId actionId)
    {
        if (_prototype.TryIndex<EntityPrototype>(actionId, out var prototype) &&
            prototype.TryGetComponent<VampireActionComponent>(out var vampireAction, _componentFactory))
        {
            return vampireAction.RequiredPowerLevel;
        }

        return VampirePowerLevel.Neonate;
    }

    private readonly record struct VampireActionChargeState(
        int CurrentCharges,
        int MaxCharges,
        float RechargeProgress);
}
