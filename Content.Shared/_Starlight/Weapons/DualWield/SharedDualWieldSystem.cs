using System;
using Content.Shared.Alert;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Weapons.DualWield;

/// <summary>
/// Управляет включением режима стрельбы по-македонски и штрафами модификаторов оружия.
/// Само чередование выстрелов обрабатывается в SharedGunSystem.
/// </summary>
public sealed class SharedDualWieldSystem : EntitySystem
{
    public static readonly ProtoId<AlertPrototype> DualWieldAlert = "DualWieldActive";

    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanDualWieldComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<GunComponent, GotEquippedHandEvent>(OnGunEquipped);
        SubscribeLocalEvent<GunComponent, GotUnequippedHandEvent>(OnGunUnequipped);
    }

    private void OnGunRefreshModifiers(Entity<CanDualWieldComponent> gun, ref GunRefreshModifiersEvent args)
    {
        if (!gun.Comp.Enabled)
            return;

        var holder = Transform(gun).ParentUid;
        if (!TryComp<DualWieldComponent>(holder, out var dualWield) ||
            !dualWield.Active ||
            dualWield.LeftGun != gun.Owner && dualWield.RightGun != gun.Owner)
            return;

        if (gun.Comp.DualWieldInaccuracyPenalty > 0f)
        {
            var penalty = Angle.FromDegrees(gun.Comp.DualWieldInaccuracyPenalty);
            args.MinAngle += penalty;
            args.MaxAngle += penalty;
        }

        if (gun.Comp.DualWieldFireRateMultiplier <= 0f)
            return;

        args.FireRate *= gun.Comp.DualWieldFireRateMultiplier;

        if (gun.Comp.DualWieldMaxFireRate > 0f)
            args.FireRate = MathF.Min(args.FireRate, gun.Comp.DualWieldMaxFireRate);
    }

    public bool ToggleDualWield(EntityUid user, EntityUid firstGun, EntityUid secondGun, bool isCurrentlyActive)
    {
        if (isCurrentlyActive)
        {
            if (TryComp<DualWieldComponent>(user, out var dualWield))
                DisableDualWield(user, dualWield, "dual-wield-disabled");

            return true;
        }

        if (!CanEnableDualWield(user, firstGun, secondGun))
        {
            _popup.PopupClient(Loc.GetString("dual-wield-popup-unavailable"), user, user);
            return false;
        }

        EnableDualWield(user, firstGun, secondGun);
        return true;
    }

    private void OnGunEquipped(Entity<GunComponent> gun, ref GotEquippedHandEvent args)
    {
        if (TryComp<DualWieldComponent>(args.User, out var dualWield) && dualWield.Active)
            return;

        if (!TryGetBothGuns(args.User, out var firstGun, out var secondGun) ||
            (gun.Owner != firstGun && gun.Owner != secondGun) ||
            !CanEnableDualWield(args.User, firstGun, secondGun))
            return;

        _popup.PopupClient(Loc.GetString("dual-wield-popup-available"), args.User, args.User);
    }

    private void OnGunUnequipped(Entity<GunComponent> gun, ref GotUnequippedHandEvent args)
    {
        if (!TryComp<DualWieldComponent>(args.User, out var dualWield) || !dualWield.Active)
            return;

        if (dualWield.LeftGun != gun.Owner && dualWield.RightGun != gun.Owner)
            return;

        DisableDualWield(args.User, dualWield, "dual-wield-interrupted");
    }

    public bool TryGetBothGuns(EntityUid user, out EntityUid firstGun, out EntityUid secondGun)
    {
        firstGun = EntityUid.Invalid;
        secondGun = EntityUid.Invalid;

        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (!HasComp<GunComponent>(held))
                continue;

            if (firstGun == EntityUid.Invalid)
            {
                firstGun = held;
                continue;
            }

            secondGun = held;
            break;
        }

        return firstGun != EntityUid.Invalid && secondGun != EntityUid.Invalid;
    }

    private bool CanDualWield(EntityUid gun)
    {
        return TryComp<CanDualWieldComponent>(gun, out var dualWield) && dualWield.Enabled;
    }

    private bool CanEnableDualWield(EntityUid user, EntityUid firstGun, EntityUid secondGun)
    {
        if (firstGun == EntityUid.Invalid || secondGun == EntityUid.Invalid || firstGun == secondGun)
            return false;

        if (!TryComp<HandsComponent>(user, out var hands))
            return false;

        if (!_hands.IsHolding((user, hands), firstGun) || !_hands.IsHolding((user, hands), secondGun))
            return false;

        return CanDualWield(firstGun) && CanDualWield(secondGun);
    }

    private void EnableDualWield(EntityUid user, EntityUid firstGun, EntityUid secondGun)
    {
        var state = EnsureComp<DualWieldComponent>(user);
        state.Active = true;
        state.LeftGun = firstGun;
        state.RightGun = secondGun;
        state.NextIsLeft = _hands.GetActiveItem(user) != secondGun;
        Dirty(user, state);

        _gun.RefreshModifiers(firstGun);
        _gun.RefreshModifiers(secondGun);
        _alerts.ShowAlert(user, DualWieldAlert);
        _popup.PopupClient(Loc.GetString("dual-wield-enabled"), user, user);
    }

    public void DisableDualWield(EntityUid user, DualWieldComponent dualWield, string? popupLocId = null)
    {
        var leftGun = dualWield.LeftGun;
        var rightGun = dualWield.RightGun;

        if (leftGun != EntityUid.Invalid)
            StopGun(leftGun);

        if (rightGun != EntityUid.Invalid && rightGun != leftGun)
            StopGun(rightGun);

        dualWield.Active = false;
        dualWield.LeftGun = EntityUid.Invalid;
        dualWield.RightGun = EntityUid.Invalid;
        dualWield.NextIsLeft = false;
        Dirty(user, dualWield);

        if (leftGun != EntityUid.Invalid)
            _gun.RefreshModifiers(leftGun);

        if (rightGun != EntityUid.Invalid && rightGun != leftGun)
            _gun.RefreshModifiers(rightGun);

        _alerts.ClearAlert(user, DualWieldAlert);

        if (popupLocId != null)
            _popup.PopupClient(Loc.GetString(popupLocId), user, user);
    }

    /// <summary>
    /// Resets the firing state for a dual-wielded gun.
    /// </summary>
    /// <param name="gun">The gun entity to reset.</param>
    private void StopGun(EntityUid gun)
    {
        _gun.ResetFireState(gun);
    }
}
