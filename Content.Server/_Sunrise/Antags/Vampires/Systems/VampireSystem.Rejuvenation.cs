using Content.Server._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires;
using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Events;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Metabolism;
using Content.Shared.Stunnable;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Antags.Vampires.Systems;

public sealed partial class VampireSystem
{
    // Омоложение и его эффекты.

    [Dependency] private readonly StatusEffectsSystem _statusEffects = null!;

    private void InitializeRejuvenation()
    {
        SubscribeLocalEvent<VampireComponent, VampireRejuvenateIActionEvent>(OnRejuvenate);
        SubscribeLocalEvent<VampireComponent, VampireRejuvenateIiActionEvent>(OnRejuvenateUpgraded);
    }

    private void OnRejuvenate(Entity<VampireComponent> ent, ref VampireRejuvenateIActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<VampireConfigurationComponent>(ent, out var configuration) ||
            !TryComp<VampireActionStateComponent>(ent, out var actionState) ||
            !actionState.Actions.TryGetValue(configuration.RejuvenateAction, out var actionEntity))
        {
            return;
        }

        if (!CheckAndConsumeBloodCost(ent, actionEntity))
            return;

        RemoveRejuvenateStuns(ent.Owner);
        args.Handled = true;
    }

    private void OnRejuvenateUpgraded(
        Entity<VampireComponent> ent,
        ref VampireRejuvenateIiActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<VampireConfigurationComponent>(ent, out var configuration) ||
            !TryComp<VampireActionStateComponent>(ent, out var actionState) ||
            !actionState.Actions.TryGetValue(configuration.RejuvenateUpgradedAction, out var actionEntity))
        {
            return;
        }

        if (!CheckAndConsumeBloodCost(ent, actionEntity))
            return;

        if (!TryGetPowerLevelPrototype(ent.Comp.PowerLevel, out var level))
            return;

        var settings = level.Rejuvenation;
        RemoveRejuvenateStuns(ent.Owner);
        PurgeRejuvenateReagents(
            ent.Owner,
            settings.ReagentPurgeAmount,
            configuration.RejuvenatePurgeMetabolismStage);
        StartRejuvenateHealing(ent.Owner, settings);
        args.Handled = true;
    }

    private void RemoveRejuvenateStuns(EntityUid uid)
    {
        _statusEffects.TryRemoveStatusEffect(uid, SharedStunSystem.StunId);
        _stun.TryUnstun(uid);
        RemComp<KnockedDownComponent>(uid);
    }

    private void PurgeRejuvenateReagents(
        EntityUid uid,
        float configuredAmount,
        ProtoId<MetabolismStagePrototype> metabolismStage)
    {
        var purgeAmount = FixedPoint2.New(configuredAmount);
        if (purgeAmount <= FixedPoint2.Zero || !TryComp<BloodstreamComponent>(uid, out var blood))
            return;

        if (!_solution.ResolveSolution(
                uid,
                blood.BloodSolutionName,
                ref blood.BloodSolution,
                out var bloodstreamSolution))
        {
            return;
        }

        var solutionEntity = blood.BloodSolution.Value;
        var removed = FixedPoint2.Zero;

        foreach (var quantity in bloodstreamSolution.Contents.ToArray())
        {
            if (removed >= purgeAmount)
                break;

            if (!_prototype.TryIndex<ReagentPrototype>(quantity.Reagent.Prototype, out var prototype) ||
                prototype.Metabolisms is null ||
                !prototype.Metabolisms.Metabolisms.ContainsKey(metabolismStage))
            {
                continue;
            }

            var removeAmount = FixedPoint2.Min(quantity.Quantity, purgeAmount - removed);
            _solution.RemoveReagent(solutionEntity, quantity.Reagent, removeAmount);
            removed += removeAmount;
        }
    }

    private void StartRejuvenateHealing(EntityUid uid, VampireRejuvenationLevelSettings settings)
    {
        if (settings.HealApplications <= 0 || settings.Healing.Empty)
            return;

        var active = EnsureComp<ActiveVampireRejuvenateComponent>(uid);
        active.ApplicationsRemaining = settings.HealApplications;
        active.ApplicationInterval = settings.HealInterval;
        active.NextApplication = _timing.CurTime;
        active.Healing = new DamageSpecifier(settings.Healing);
    }

    private void ProcessActiveRejuvenation(TimeSpan now)
    {
        var query = EntityQueryEnumerator<ActiveVampireRejuvenateComponent>();
        while (query.MoveNext(out var uid, out var rejuvenate))
        {
            if (now < rejuvenate.NextApplication)
                continue;

            _damageable.TryChangeDamage(uid, rejuvenate.Healing, true);
            rejuvenate.ApplicationsRemaining--;

            if (rejuvenate.ApplicationsRemaining <= 0)
            {
                RemCompDeferred<ActiveVampireRejuvenateComponent>(uid);
                continue;
            }

            rejuvenate.NextApplication = now + rejuvenate.ApplicationInterval;
        }
    }
}
