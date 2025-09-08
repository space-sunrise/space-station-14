using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Conditions;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Random;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    private void InitializeCondition()
    {
        SubscribeLocalEvent<WhitelistTriggerConditionComponent, AttemptTriggerEvent>(OnWhitelistTriggerAttempt);

        SubscribeLocalEvent<UseDelayTriggerConditionComponent, AttemptTriggerEvent>(OnUseDelayTriggerAttempt);

        SubscribeLocalEvent<ToggleTriggerConditionComponent, AttemptTriggerEvent>(OnToggleTriggerAttempt);
        SubscribeLocalEvent<ToggleTriggerConditionComponent, GetVerbsEvent<AlternativeVerb>>(OnToggleGetAltVerbs);

        SubscribeLocalEvent<RandomChanceTriggerConditionComponent, AttemptTriggerEvent>(OnRandomChanceTriggerAttempt);
        SubscribeLocalEvent<StartTimerOnShootComponent, ProjectileShotEvent>(StartTimerOnShoot); // Sunrise-Edit
    }

    // Sunrise-Start
    private void StartTimerOnShoot(EntityUid uid, StartTimerOnShootComponent component, ProjectileShotEvent args)
    {
        if (TryComp<ProjectileComponent>(uid, out var projectile))
            ActivateTimerTrigger(uid, projectile.Shooter);
    }
    // Sunrise-End

    private void OnWhitelistTriggerAttempt(Entity<WhitelistTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key == null || ent.Comp.Keys.Contains(args.Key))
            args.Cancelled |= !_whitelist.CheckBoth(args.User, ent.Comp.UserBlacklist, ent.Comp.UserWhitelist);
    }

    private void OnUseDelayTriggerAttempt(Entity<UseDelayTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key == null || ent.Comp.Keys.Contains(args.Key))
            args.Cancelled |= _useDelay.IsDelayed(ent.Owner, ent.Comp.UseDelayId);
    }

    private void OnToggleTriggerAttempt(Entity<ToggleTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key == null || ent.Comp.Keys.Contains(args.Key))
            args.Cancelled |= !ent.Comp.Enabled;
    }

    private void OnToggleGetAltVerbs(Entity<ToggleTriggerConditionComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString(ent.Comp.ToggleVerb),
            Act = () => Toggle(ent, user)
        });
    }

    private void Toggle(Entity<ToggleTriggerConditionComponent> ent, EntityUid user)
    {
        var msg = ent.Comp.Enabled ? ent.Comp.ToggleOff : ent.Comp.ToggleOn;
        _popup.PopupPredicted(Loc.GetString(msg), ent.Owner, user);
        ent.Comp.Enabled = !ent.Comp.Enabled;
        Dirty(ent);
    }

    private void OnRandomChanceTriggerAttempt(Entity<RandomChanceTriggerConditionComponent> ent,
        ref AttemptTriggerEvent args)
    {
        if (args.Key == null || ent.Comp.Keys.Contains(args.Key))
        {
            // TODO: Replace with RandomPredicted once the engine PR is merged
            var hash = new List<int>
            {
                (int)_timing.CurTick.Value,
                GetNetEntity(ent).Id,
                args.User == null ? 0 : GetNetEntity(args.User.Value).Id,
            };
            var seed = SharedRandomExtensions.HashCodeCombine(hash);
            var rand = new System.Random(seed);

            args.Cancelled |= !rand.Prob(ent.Comp.SuccessChance); // When not successful, Cancelled = true
        }
    }
}
