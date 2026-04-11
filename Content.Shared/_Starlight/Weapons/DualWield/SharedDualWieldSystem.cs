using System;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._Starlight.Weapons.DualWield;

/// <summary>
/// Управляет включением режима стрельбы по-македонски и штрафами модификаторов оружия.
/// Само чередование выстрелов обрабатывается в SharedGunSystem.
/// </summary>
public sealed class SharedDualWieldSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanDualWieldComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
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

    public void ToggleDualWield(EntityUid user, EntityUid firstGun, EntityUid secondGun, bool isCurrentlyActive)
    {
        if (isCurrentlyActive)
        {
            if (TryComp<DualWieldComponent>(user, out var dualWield))
            {
                dualWield.Active = false;
                Dirty(user, dualWield);
                _gun.RefreshModifiers(dualWield.LeftGun);
                _gun.RefreshModifiers(dualWield.RightGun);
            }

            _popup.PopupClient(Loc.GetString("dual-wield-disabled"), user, user);
            return;
        }

        if (!CanDualWield(firstGun) || !CanDualWield(secondGun))
        {
            _popup.PopupClient(Loc.GetString("dual-wield-too-heavy"), user, user);
            return;
        }

        var state = EnsureComp<DualWieldComponent>(user);
        state.Active = true;
        state.LeftGun = firstGun;
        state.RightGun = secondGun;
        state.NextIsLeft = _hands.GetActiveItem(user) == firstGun;
        Dirty(user, state);

        _gun.RefreshModifiers(firstGun);
        _gun.RefreshModifiers(secondGun);
        _popup.PopupClient(Loc.GetString("dual-wield-enabled"), user, user);
    }

    private void OnGunUnequipped(Entity<GunComponent> gun, ref GotUnequippedHandEvent args)
    {
        if (!TryComp<DualWieldComponent>(args.User, out var dualWield) || !dualWield.Active)
            return;

        if (dualWield.LeftGun != gun.Owner && dualWield.RightGun != gun.Owner)
            return;

        dualWield.Active = false;
        Dirty(args.User, dualWield);

        var otherGun = dualWield.LeftGun == gun.Owner ? dualWield.RightGun : dualWield.LeftGun;
        _gun.RefreshModifiers(gun.Owner);
        _gun.RefreshModifiers(otherGun);
        _popup.PopupClient(Loc.GetString("dual-wield-interrupted"), args.User, args.User);
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
}
