using System.Diagnostics.CodeAnalysis;
using Content.Shared.Alert;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Weapons.DualWield;

/// <summary>
///     Manages activation and deactivation of dual-wielding based on equipped weapons.
///     Applies dual-wield penalties via GunRefreshModifiersEvent.
/// </summary>
public sealed class SharedDualWieldSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandsComponent, DidEquipHandEvent>(OnHandEquipped);
        SubscribeLocalEvent<HandsComponent, DidUnequipHandEvent>(OnHandUnequipped);
        SubscribeLocalEvent<HandsComponent, HandCountChangedEvent>(OnHandCountChanged);
        SubscribeLocalEvent<DualWieldComponent, ComponentShutdown>(OnDualWieldShutdown);
        SubscribeLocalEvent<CanDualWieldComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
    }

    private void OnHandEquipped(Entity<HandsComponent> ent, ref DidEquipHandEvent args)
    {
        if (_timing.ApplyingState)
            return;

        CheckAndUpdateDualWield(ent.AsNullable());
    }

    private void OnHandUnequipped(Entity<HandsComponent> ent, ref DidUnequipHandEvent args)
    {
        if (_timing.ApplyingState)
            return;

        CheckAndUpdateDualWield(ent());
    }

    private void OnHandCountChanged(Entity<HandsComponent> ent, ref HandCountChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        CheckAndUpdateDualWield(ent.AsNullable());
    }

    private void OnDualWieldShutdown(Entity<DualWieldComponent> ent, ref ComponentShutdown args)
    {
        ClearDualWieldAlerts(ent, ent.Comp.LeftGun, ent.Comp.RightGun);
        RefreshDualWieldGuns(ent.Comp.LeftGun, ent.Comp.RightGun);
    }

    private void CheckAndUpdateDualWield(Entity<HandsComponent?> ent)
    {
        if (!TryGetBothDualWieldGuns(ent, out var leftGun, out var rightGun))
        {
            DisableDualWield(ent);
            return;
        }

        EnableDualWield(ent, leftGun, rightGun);
    }

    private bool TryGetBothDualWieldGuns(Entity<HandsComponent?> ent, [NotNullWhen(true)] out EntityUid? leftGun, [NotNullWhen(true)] out EntityUid? rightGun)
    {
        leftGun = null;
        rightGun = null;

        if (!Resolve(ent, ref ent.Comp))
            return false;

        foreach (var handName in _hands.EnumerateHands(ent))
        {
            var held = _hands.GetHeldItem(ent, handName);

            if (!HasComp<CanDualWieldComponent>(held.Value))
                continue;

            if (!_hands.TryGetHand(ent, handName, out var hand))
                continue;

            switch (hand.Value.Location)
            {
                case HandLocation.Left:
                    leftGun = held;
                    break;
                case HandLocation.Right:
                    rightGun = held;
                    break;
            }
        }

        if (leftGun == null || rightGun == null)
            return false;

        if (!TryComp<CanDualWieldComponent>(leftGun.Value, out var leftDualWield))
            return false;

        if (!TryComp<CanDualWieldComponent>(rightGun.Value, out var rightDualWield))
            return false;

        if (leftDualWield.HandsRequired != rightDualWield.HandsRequired)
            return false;

        return ent.Comp.Count == leftDualWield.HandsRequired;
    }

    private void EnableDualWield(Entity<HandsComponent?> ent, EntityUid? leftGun, EntityUid? rightGun)
    {

        if (TryComp<DualWieldComponent>(ent.Owner, out var existingDualWield) &&
            existingDualWield.LeftGun == leftGun &&
            existingDualWield.RightGun == rightGun)
            return;

        DisableDualWield(ent);

        var dualWield = EnsureComp<DualWieldComponent>(ent);
        dualWield.LeftGun = leftGun.Value;
        dualWield.RightGun = rightGun.Value;
        dualWield.GunQueue = new List<EntityUid> { leftGun.Value, rightGun.Value };
        Dirty(ent, dualWield);

        ShowDualWieldAlert(ent, leftGun.Value, rightGun.Value);
        RefreshDualWieldGuns(leftGun, rightGun);
    }

    private void DisableDualWield(EntityUid uid)
    {
        RemComp<DualWieldComponent>(uid);
    }

    /// <summary>
    ///     Applies dual-wield penalties to gun modifiers when the weapon's stats are refreshed.
    /// </summary>
    private void OnGunRefreshModifiers(Entity<CanDualWieldComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var wielder = Transform(ent).ParentUid;
        if (wielder == EntityUid.Invalid)
            return;

        if (!TryComp<DualWieldComponent>(wielder, out var dualWield))
            return;

        if (dualWield.LeftGun != ent.Owner && dualWield.RightGun != ent.Owner)
            return;

        args.AngleIncrease *= (1f + ent.Comp.DualWieldInaccuracyPenalty);
        // Clamp penalty to [0, 0.99] so FireRate can never reach zero or go negative.
        var clampedPenalty = Math.Clamp(ent.Comp.DualWieldFireRatePenalty, 0f, 0.99f);
        args.FireRate *= (1f - clampedPenalty);
        // Safety floor: keep fire rate positive so downstream logic never divides by zero.
        args.FireRate = MathF.Max(args.FireRate, 0.1f);
        args.CameraRecoilScalar *= (1f + ent.Comp.DualWieldRecoilPenalty);
    }

    private void ShowDualWieldAlert(EntityUid uid, EntityUid leftGun, EntityUid rightGun)
    {
        if (TryGetDualWieldAlert(leftGun, rightGun, out var alert))
            _alerts.ShowAlert(uid, alert, severity: 0);
    }

    private bool TryGetDualWieldAlert(EntityUid leftGun, EntityUid rightGun, out ProtoId<AlertPrototype> alert)
    {
        alert = default;

        TryComp<CanDualWieldComponent>(leftGun, out var leftDualWield);
        TryComp<CanDualWieldComponent>(rightGun, out var rightDualWield);

        if (leftDualWield != null && rightDualWield != null)
        {
            if (leftDualWield.DualWieldAlert != rightDualWield.DualWieldAlert)
                return false;

            alert = leftDualWield.DualWieldAlert;
            return true;
        }

        if (leftDualWield != null)
        {
            alert = leftDualWield.DualWieldAlert;
            return true;
        }

        if (rightDualWield == null)
            return false;

        alert = rightDualWield.DualWieldAlert;
        return true;
    }

    private void ClearDualWieldAlerts(EntityUid uid, EntityUid? leftGun, EntityUid? rightGun)
    {
        var leftDualWield = leftGun.HasValue &&
                            TryComp<CanDualWieldComponent>(leftGun.Value, out var leftDualWieldComp)
            ? leftDualWieldComp
            : null;

        if (leftDualWield != null)
        {
            _alerts.ClearAlert(uid, leftDualWield.DualWieldAlert);
        }

        if (rightGun.HasValue &&
            TryComp<CanDualWieldComponent>(rightGun.Value, out var rightDualWield) &&
            leftDualWield?.DualWieldAlert != rightDualWield.DualWieldAlert)
        {
            _alerts.ClearAlert(uid, rightDualWield.DualWieldAlert);
        }
    }

    private void RefreshDualWieldGuns(EntityUid? leftGun, EntityUid? rightGun)
    {
        if (leftGun != null)
            _gunSystem.RefreshModifiers(leftGun.Value);

        if (rightGun != null)
            _gunSystem.RefreshModifiers(rightGun.Value);
    }
}
