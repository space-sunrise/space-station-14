using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Content.Shared.Hands;

namespace Content.Shared._Starlight.Weapons.DualWield;

public abstract class SharedDualWieldSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string DualWieldAlertKey = "DualWieldActive";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandsComponent, GotEquippedHandEvent>(OnHandEquipped);
        SubscribeLocalEvent<HandsComponent, GotUnequippedHandEvent>(OnHandUnequipped);
    }

    private void OnHandEquipped(EntityUid uid, HandsComponent component, GotEquippedHandEvent args)
    {
        if (_timing.ApplyingState) return;
        CheckAndUpdateDualWield(uid);
    }

    private void OnHandUnequipped(EntityUid uid, HandsComponent component, GotUnequippedHandEvent args)
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

            if (handName == "left")
                leftGun = held;
            else if (handName == "right")
                rightGun = held;
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
    }

    private void DisableDualWield(EntityUid uid)
    {
        if (!TryComp<DualWieldComponent>(uid, out var dualWield))
            return;

        dualWield.Active = false;
        dualWield.LeftGun = dualWield.RightGun = null;
        Dirty(uid, dualWield);

        _alerts.ClearAlert(uid, DualWieldAlertKey);
    }
}
