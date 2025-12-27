using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Shared._Sunrise.Abilities.Felinid;

public sealed class FelinidLickingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

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
        if (args.Handled || args.Target == null) // мда
            return;

        var target = args.Target;

        if (!CanLick(uid, target, component, out var damageable, out var errorMessage))
        {
            if (errorMessage != null)
                _popup.PopupClient(errorMessage, uid, uid);
            return;
        }

        StartLicking(uid, target, component, damageable!);
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

        _damageable.TryChangeDamage(target, component.Damage, true, origin: uid);

        if (component.StopBleeding && TryComp<BloodstreamComponent>(target, out var bloodstream))
        {
            var wasBleeding = bloodstream.BleedAmount > 0;
            _bloodstreamSystem.TryModifyBleedAmount((target, bloodstream), component.BloodlossModifier);

            if (wasBleeding && bloodstream.BleedAmount <= 0)
            {
                var popup = (uid == target)
                    ? Loc.GetString("medical-item-stop-bleeding-self")
                    : Loc.GetString("medical-item-stop-bleeding", ("target", Identity.Entity(target, EntityManager)));
                _popup.PopupClient(popup, target, uid);
            }
        }

        _audio.PlayPredicted(component.HealingEndSound, uid, uid);

        if (_mobStateSystem.IsAlive(target) && HasDamageToHeal(target, damageable, component))
        {
            StartLicking(uid, target, component, damageable);
        }

        args.Handled = true;
    }

    /// <summary>
    /// Проверяет, можно ли облизывать раны цели
    /// </summary>
    /// <param name="licker">Тот, кто облизывает</param>
    /// <param name="target">Цель</param>
    /// <param name="component">Компонент облизывания</param>
    /// <param name="damageable">Компонент урона цели (если доступен)</param>
    /// <param name="errorMessage">Сообщение об ошибке (если есть)</param>
    /// <returns>True, если можно облизывать</returns>
    private bool CanLick(EntityUid licker, EntityUid target, FelinidLickingComponent component,
        [NotNullWhen(true)] out DamageableComponent? damageable, out string? errorMessage)
    {
        damageable = null;
        errorMessage = null;

        if (_standing.IsDown(licker))
            return false;

        if (!TryComp<DamageableComponent>(target, out damageable))
            return false;

        if (!_mobStateSystem.IsAlive(target))
            return false;

        if (HasIngestionBlocker(licker))
        {
            errorMessage = Loc.GetString("felinid-licking-blocked-by-blocker");
            return false;
        }

        if (HasInnerOrOuterClothing(target))
        {
            errorMessage = Loc.GetString("felinid-licking-blocked-by-clothing");
            return false;
        }

        if (!HasDamageToHeal(target, damageable, component))
            return false;

        return true;
    }

    /// <summary>
    /// Проверяет, есть ли у сущности урон, который можно вылечить
    /// </summary>
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

    /// <summary>
    /// Проверяет, есть ли у сущности блокировщик приема пищи (маска или шлем)
    /// </summary>
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

    /// <summary>
    /// Проверяет, есть ли у сущности внутренняя или внешняя одежда
    /// </summary>
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
