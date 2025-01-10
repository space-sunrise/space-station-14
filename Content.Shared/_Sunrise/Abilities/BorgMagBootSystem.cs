using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Slippery;
using Robust.Shared.Network;

namespace Content.Shared._Sunrise.Abilities;

public sealed class SharedBorgMagbootsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _sharedActions = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BorgMagbootsComponent, SlipAttemptEvent>(OnSlipAttempt);
        SubscribeLocalEvent<BorgMagbootsComponent, ToggleBorgMagbootsActionEvent>(OnToggleAction);
        SubscribeLocalEvent<BorgMagbootsComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<BorgMagbootsComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, BorgMagbootsComponent component, MapInitEvent args)
    {
        _sharedActions.AddAction(uid, ref component.ToggleActionEntity, component.ToggleAction);
    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, BorgMagbootsComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        var walkMod = 1f;
        var sprintMod = 1f;
        if (component.On)
        {
            walkMod = component.WalkModifier;
            sprintMod = component.SprintModifier;
        }

        args.ModifySpeed(walkMod, sprintMod);
    }

    private void OnToggleAction(Entity<BorgMagbootsComponent> ent, ref ToggleBorgMagbootsActionEvent args)
    {
        if (args.Handled)
            return;

        ToggleMagboots(ent);

        args.Handled = true;
    }

    private void ToggleMagboots(Entity<BorgMagbootsComponent> ent)
    {
        ent.Comp.On = !ent.Comp.On;

        UpdateMagbootEffects(ent.Owner, ent, ent.Comp.On);
        _sharedActions.SetToggled(ent.Comp.ToggleActionEntity, ent.Comp.On);
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(ent.Owner);
        Dirty(ent);
    }

    public void UpdateMagbootEffects(EntityUid user, Entity<BorgMagbootsComponent> ent, bool state)
    {
        // TODO: public api for this and add access
        if (TryComp<MovedByPressureComponent>(user, out var moved))
            moved.Enabled = !state;

        if (state)
            _alerts.ShowAlert(user, ent.Comp.MagbootsAlert);
        else
            _alerts.ClearAlert(user, ent.Comp.MagbootsAlert);
    }

    private void OnSlipAttempt(EntityUid uid, BorgMagbootsComponent component, SlipAttemptEvent args)
    {
        if (component.On)
            args.NoSlip = true;
    }
}
