using Content.Shared.Interaction.Events;
using Content.Shared.Sunrise.FactionGunBlockerSystem;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;

namespace Content.Client._Sunrise.FactionWeaponBlockerSystem;

public sealed class FactionWeaponBlockerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionWeaponBlockerComponent, AttemptShootEvent>(OnShootAttempt);
        SubscribeLocalEvent<FactionWeaponBlockerComponent, AttemptMeleeEvent>(OnMeleeAttempt);
        SubscribeLocalEvent<FactionWeaponBlockerComponent, UseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<FactionWeaponBlockerComponent, InteractionAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<FactionWeaponBlockerComponent, ComponentHandleState>(OnFactionWeaponBlockerHandleState);
    }

    private void OnUseAttempt(EntityUid uid, FactionWeaponBlockerComponent component, ref UseAttemptEvent args)
    {
        if (component.CanUse)
            return;

        args.Cancel();
    }

    private void OnInteractAttempt(EntityUid uid, FactionWeaponBlockerComponent component, ref InteractionAttemptEvent args)
    {
        if (component.CanUse)
            return;

        args.Cancel();
    }

    private void OnFactionWeaponBlockerHandleState(EntityUid uid, FactionWeaponBlockerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not FactionWeaponBlockerComponentState state)
            return;

        component.CanUse = state.CanUse;
        component.AlertText = state.AlertText;
    }

    private void OnMeleeAttempt(EntityUid uid, FactionWeaponBlockerComponent component, ref AttemptMeleeEvent args)
    {
        if (component.CanUse)
            return;

        args.Cancelled = true;
        args.Message = component.AlertText;
    }

    private void OnShootAttempt(EntityUid uid, FactionWeaponBlockerComponent component, ref AttemptShootEvent args)
    {
        if (component.CanUse)
            return;

        args.Cancelled = true;
        args.Message = component.AlertText;
    }
}
