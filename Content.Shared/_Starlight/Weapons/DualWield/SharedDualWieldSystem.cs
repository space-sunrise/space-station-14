using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Content.Shared.Hands;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Weapons.DualWield;

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

    private static readonly ProtoId<AlertPrototype> DualWieldAlertKey = "DualWieldActive";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandsComponent, DidEquipHandEvent>(OnHandEquipped);
        SubscribeLocalEvent<HandsComponent, DidUnequipHandEvent>(OnHandUnequipped);
        SubscribeLocalEvent<CanDualWieldComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
    }

    private void OnHandEquipped(EntityUid uid, HandsComponent component, DidEquipHandEvent args)
    {
        if (_timing.ApplyingState) return;
        CheckAndUpdateDualWield(uid);
    }

    private void OnHandUnequipped(EntityUid uid, HandsComponent component, DidUnequipHandEvent args)
    {
        if (_timing.ApplyingState) return;
        CheckAndUpdateDualWield(uid);
    }

    private void CheckAndUpdateDualWield(EntityUid uid)
    {
        if (!TryGetBothDualWieldGuns(uid, out var leftGun, out var rightGun))
        {
            DisableDualWield(uid);
            return;
        }

        EnableDualWield(uid, leftGun!.Value, rightGun!.Value);
    }

    private bool TryGetBothDualWieldGuns(EntityUid uid, [NotNullWhen(true)] out EntityUid? leftGun, [NotNullWhen(true)] out EntityUid? rightGun)
    {
        leftGun = null;
        rightGun = null;

        if (!TryComp<HandsComponent>(uid, out var handsComp))
            return false;

        var entity = new Entity<HandsComponent?>(uid, handsComp);

        foreach (var handName in _hands.EnumerateHands(entity))
        {
            var held = _hands.GetHeldItem(entity, handName);
            if (held == null)
                continue;

            if (!HasComp<CanDualWieldComponent>(held.Value))
                continue;

            if (!_hands.TryGetHand(entity, handName, out var hand))
                continue;

            switch (hand.Location)
            {
                case HandLocation.Left:
                    leftGun = held;
                    break;
                case HandLocation.Right:
                    rightGun = held;
                    break;
            }
        }

        return leftGun != null && rightGun != null;
    }

    private void EnableDualWield(EntityUid uid, EntityUid leftGun, EntityUid rightGun)
    {
        var dualWield = EnsureComp<DualWieldComponent>(uid);
        dualWield.Active = true;
        dualWield.LeftGun = leftGun;
        dualWield.RightGun = rightGun;
        dualWield.NextIsLeft = true;
        Dirty(uid, dualWield);

        _alerts.ShowAlert(uid, DualWieldAlertKey);

        // Force refresh modifiers for both guns to apply penalties immediately
        _gunSystem.RefreshModifiers(leftGun);
        _gunSystem.RefreshModifiers(rightGun);
    }

    private void DisableDualWield(EntityUid uid)
    {
        if (!TryComp<DualWieldComponent>(uid, out var dualWield))
            return;

        var leftGun = dualWield.LeftGun;
        var rightGun = dualWield.RightGun;

        dualWield.Active = false;
        dualWield.LeftGun = dualWield.RightGun = null;
        Dirty(uid, dualWield);

        _alerts.ClearAlert(uid, DualWieldAlertKey);

        // Refresh modifiers to remove penalties
        if (leftGun != null)
            _gunSystem.RefreshModifiers(leftGun.Value);
        if (rightGun != null)
            _gunSystem.RefreshModifiers(rightGun.Value);
    }

    /// <summary>
    ///     Applies dual-wield penalties to gun modifiers when the weapon's stats are refreshed.
    /// </summary>
    private void OnGunRefreshModifiers(Entity<CanDualWieldComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var holder = Transform(ent).ParentUid;
        if (holder == EntityUid.Invalid)
            return;

        if (!TryComp<DualWieldComponent>(holder, out var dualWield) || !dualWield.Active)
            return;

        if (dualWield.LeftGun != ent.Owner && dualWield.RightGun != ent.Owner)
            return;

        args.AngleIncrease *= (1f + ent.Comp.DualWieldInaccuracyPenalty);
        args.FireRate *= (1f - ent.Comp.DualWieldFireRatePenalty);
        args.CameraRecoilScalar *= (1f + ent.Comp.DualWieldRecoilPenalty);
    }
}
