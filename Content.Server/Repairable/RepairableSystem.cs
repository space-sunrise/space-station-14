using Content.Server.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Repairable;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Server.Repairable
{
    public sealed class RepairableSystem : SharedRepairableSystem
    {
        [Dependency] private readonly SharedToolSystem _toolSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger= default!;
        [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<RepairableComponent, InteractUsingEvent>(Repair);
            SubscribeLocalEvent<RepairableComponent, RepairFinishedEvent>(OnRepairFinished);
        }

        private void OnRepairFinished(EntityUid uid, RepairableComponent component, RepairFinishedEvent args)
        {
            if (args.Cancelled)
                return;

            if (!EntityManager.TryGetComponent(uid, out DamageableComponent? damageable) || damageable.TotalDamage == 0)
                return;

            if (component.Damage != null)
            {
                // Sunrise-start
                var damageChanged = _damageableSystem.TryChangeDamage(uid, component.Damage, true, false, origin: args.User);
                _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} by {damageChanged?.GetTotal()}");

                if (CanRepair(damageable.Damage.DamageDict, component) && args.Used != null)
                {
                    var isNotSelf = args.User != args.Target;

                    var delay = isNotSelf
                        ? component.DoAfterDelay
                        : component.DoAfterDelay * GetScaledRepairPenalty(args.User, component);

                    _toolSystem.UseTool(args.Used.Value, args.User, uid, delay, component.QualityNeeded, new RepairFinishedEvent(), component.FuelCost);
                }
                // Sunrise-end
            }
            else
            {
                // Repair all damage
                _damageableSystem.SetAllDamage(uid, damageable, 0);
                _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} back to full health");
            }

            var str = Loc.GetString("comp-repairable-repair",
                ("target", uid),
                ("tool", args.Used!));
            _popup.PopupEntity(str, uid, args.User);

            var ev = new RepairedEvent((uid, component), args.User);
            RaiseLocalEvent(uid, ref ev);
        }

        // Sunrise-start
        private bool CanRepair(Dictionary<string, FixedPoint2> damage, RepairableComponent component)
        {
            if (component.Damage == null)
            {
                return true;
            }

            foreach (var type in component.Damage.DamageDict)
            {
                if (damage[type.Key].Value > 0)
                {
                    return true;
                }
            }

            return false;
        }
        // Sunrise-end

        public async void Repair(EntityUid uid, RepairableComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            // Only try repair the target if it is damaged
            if (!TryComp<DamageableComponent>(uid, out var damageable) || damageable.TotalDamage == 0)
                return;

            if (!component.AllowSelfRepair && args.User == uid)
                return;

            // Sunrise-start
            if (!CanRepair(damageable.Damage.DamageDict, component))
                return;
            // Sunrise-end

            // Add a penalty to how long it takes if the user is repairing itself
            var isNotSelf = args.User != args.Target;

            var delay = isNotSelf
                ? component.DoAfterDelay
                : component.DoAfterDelay * GetScaledRepairPenalty(args.User, component);

            // Run the repairing doafter
            args.Handled = _toolSystem.UseTool(args.Used, args.User, uid, delay, component.QualityNeeded, new RepairFinishedEvent(), component.FuelCost);
        }

        public float GetScaledRepairPenalty(EntityUid uid, RepairableComponent component)
        {
            var output = component.DoAfterDelay;
            if (!TryComp<MobThresholdsComponent>(uid, out var mobThreshold) ||
                !TryComp<DamageableComponent>(uid, out var damageable))
                return output;
            if (!_mobThresholdSystem.TryGetThresholdForState(uid, MobState.Critical, out var amount, mobThreshold))
                return 1;

            var percentDamage = (float) (damageable.TotalDamage / amount);
            //basically make it scale from 1 to the multiplier.
            var modifier = percentDamage * (component.SelfRepairPenalty - 1) + 1;
            return Math.Max(modifier, 1);
        }
    }

    /// <summary>
    /// Event raised on an entity when its successfully repaired.
    /// </summary>
    /// <param name="Ent"></param>
    /// <param name="User"></param>
    [ByRefEvent]
    public readonly record struct RepairedEvent(Entity<RepairableComponent> Ent, EntityUid User);

}
