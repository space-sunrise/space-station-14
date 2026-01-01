using System.Linq;
using Content.Shared.Hands;
using Content.Shared.NPC.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._Sunrise.FactionWeaponBlockerSystem;

public sealed class SharedFactionWeaponBlockerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionWeaponBlockerComponent, AttemptShootEvent>(OnShootAttempt);
        SubscribeLocalEvent<FactionWeaponBlockerComponent, AttemptMeleeEvent>(OnMeleeAttempt);

        SubscribeLocalEvent<FactionWeaponBlockerComponent, GotEquippedHandEvent>(OnGotEquippedHand);
    }

    private void OnGotEquippedHand(Entity<FactionWeaponBlockerComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!TryComp<NpcFactionMemberComponent>(args.User, out var npcFactionMemberComponent))
            return;

        var canUse = npcFactionMemberComponent.Factions
            .Any(x => ent.Comp.Factions.Contains(x));

        if (ent.Comp.CanUse == canUse)
            return;

        ent.Comp.CanUse = canUse;
        Dirty(ent);
    }

    private void OnMeleeAttempt(Entity<FactionWeaponBlockerComponent> ent, ref AttemptMeleeEvent args)
    {
        if (ent.Comp.CanUse)
            return;

        args.Cancelled = true;
        args.Message = Loc.GetString(ent.Comp.AlertText);
    }

    private void OnShootAttempt(Entity<FactionWeaponBlockerComponent> ent, ref AttemptShootEvent args)
    {
        if (ent.Comp.CanUse)
            return;

        args.Cancelled = true;
        args.Message = Loc.GetString(ent.Comp.AlertText);
    }
}
