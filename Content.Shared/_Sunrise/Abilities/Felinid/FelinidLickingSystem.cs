using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Abilities.Felinid;

public sealed class FelinidLickingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FelinidLickingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FelinidLickingComponent, LickingWoundsTargetActionEvent>(OnLickingAction);
        SubscribeLocalEvent<FelinidLickingComponent, FelinidLickingDoAfterEvent>(OnDoAfter);
    }

    private void OnStartup(EntityUid uid, FelinidLickingComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, component.ActionLickingWoundsId);
    }

    private void OnLickingAction(EntityUid uid, FelinidLickingComponent component, LickingWoundsTargetActionEvent args)
    {
        if (args.Handled)
            return;

        if (_standing.IsDown(uid))
            return;

        var target = args.Target;
        if (target == null)
            return;

        if (HasIngestionBlocker(uid))
        {
            _popup.PopupClient(Loc.GetString("felinid-licking-blocked-by-blocker"), uid, uid);
            args.Handled = true;
            return;
        }

        if (HasInnerOrOuterClothing(target))
        {
            _popup.PopupClient(Loc.GetString("felinid-licking-blocked-by-clothing"), uid, uid);
            args.Handled = true;
            return;
        }

        if (!TryComp<DamageableComponent>(target, out var damageable))
            return;

        StartLicking(uid, target, component, damageable);
        args.Handled = true;
    }

    private void StartLicking(EntityUid uid, EntityUid target, FelinidLickingComponent licking, DamageableComponent damageable)
    {
        _audio.PlayPredicted(licking.HealingBeginSound, uid, uid);

        var doAfterArgs = new DoAfterArgs(EntityManager, uid, licking.Delay, new FelinidLickingDoAfterEvent(), uid, target: target)
        {
            BreakOnMove = true,
            NeedHand = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(EntityUid uid, FelinidLickingComponent component, FelinidLickingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!TryComp<DamageableComponent>(target, out var damageable))
            return;

        var healed = _damageable.TryChangeDamage(target, component.Damage, true, origin: uid);

        if (component.StopBleeding && TryComp<BloodstreamComponent>(target, out var bloodstream))
        {
            var isBleeding = bloodstream.BleedAmount > 0;
            _bloodstreamSystem.TryModifyBleedAmount((target, bloodstream), component.BloodlossModifier);

            if (isBleeding != bloodstream.BleedAmount > 0)
            {
                var popup = (uid == target)
                    ? Loc.GetString("medical-item-stop-bleeding-self")
                    : Loc.GetString("medical-item-stop-bleeding", ("target", Identity.Entity(target, EntityManager)));
                _popup.PopupClient(popup, target, uid);
            }
        }

        _audio.PlayPredicted(component.HealingEndSound, uid, uid);

        if (HasDamageToHeal(target, damageable, component))
        {
            StartLicking(uid, target, component, damageable);
        }
        else
        {
            _popup.PopupClient("felinid-licking-stop", uid, uid);
        }

        args.Handled = true;
    }

    private bool HasDamageToHeal(EntityUid target, DamageableComponent damageable, FelinidLickingComponent licking)
    {
        foreach (var (type, amount) in licking.Damage.DamageDict)
        {
            if (damageable.Damage.DamageDict.TryGetValue(type, out var currentDamage) &&
                currentDamage > FixedPoint2.Zero)
            {
                return true;
            }
        }

        if (licking.StopBleeding && TryComp<BloodstreamComponent>(target, out var bloodstream))
        {
            if (bloodstream.BleedAmount > 0)
                return true;
        }

        return false;
    }

    private bool HasIngestionBlocker(EntityUid uid)
    {
        if (!TryComp<InventoryComponent>(uid, out var inventory))
            return false;

        var enumerator = _inventorySystem.GetSlotEnumerator((uid, inventory), SlotFlags.MASK | SlotFlags.HEAD);
        while (enumerator.NextItem(out var item))
        {
            if (TryComp<IngestionBlockerComponent>(item, out var blocker) && blocker.Enabled)
                return true;
        }
        return false;
    }

    private bool HasInnerOrOuterClothing(EntityUid uid)
    {
        if (!TryComp<InventoryComponent>(uid, out var inventory))
            return false;

        var enumerator = _inventorySystem.GetSlotEnumerator((uid, inventory), SlotFlags.INNERCLOTHING | SlotFlags.OUTERCLOTHING);
        return enumerator.NextItem(out _);
    }
}

[Serializable, NetSerializable]
public sealed partial class FelinidLickingDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class LickingWoundsTargetActionEvent : EntityTargetActionEvent
{
}
