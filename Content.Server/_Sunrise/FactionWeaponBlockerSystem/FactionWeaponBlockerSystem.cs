using Content.Shared.Hands;
using Content.Shared.NPC.Components;
using Content.Shared.Sunrise.FactionGunBlockerSystem;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;

namespace Content.Server._Sunrise.FactionWeaponBlockerSystem;

public sealed class FactionWeaponBlockerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionWeaponBlockerComponent, AttemptShootEvent>(OnShootAttempt);
        SubscribeLocalEvent<FactionWeaponBlockerComponent, AttemptMeleeEvent>(OnMeleeAttempt);
        SubscribeLocalEvent<FactionWeaponBlockerComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<FactionWeaponBlockerComponent, GotEquippedHandEvent>(OnGotEquippedHand);
    }

    private void OnGotEquippedHand(EntityUid uid, FactionWeaponBlockerComponent component, GotEquippedHandEvent args)
    {
        var canUse = false;
        if (TryComp<NpcFactionMemberComponent>(args.User, out var npcFactionMemberComponent))
        {
            foreach (var faction in npcFactionMemberComponent.Factions)
            {
                if (component.Factions.Contains(faction))
                    canUse = true;
            }
        }

        component.CanUse = canUse;
        Dirty(uid, component);
    }

    private void OnGetState(EntityUid uid, FactionWeaponBlockerComponent component, ref ComponentGetState args)
    {
        args.State = new FactionWeaponBlockerComponentState()
        {
            CanUse = component.CanUse,
            AlertText = component.AlertText
        };
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
