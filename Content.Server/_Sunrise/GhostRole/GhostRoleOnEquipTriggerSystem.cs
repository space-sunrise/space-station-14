using Content.Shared.GhostRole.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;

namespace Content.Server.GhostRole;

public sealed class GhostRoleOnEquipTriggerSystem : EntitySystem
{
    [Dependency] private readonly MakeGhostRoleOnTriggerSystem _makeGhostRole = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostRoleOnEquipTriggerComponent, ClothingGotEquippedEvent>(OnClothingGotEquipped);
        SubscribeLocalEvent<GhostRoleOnEquipTriggerComponent, ClothingGotUnequippedEvent>(OnClothingGotUnequipped);
        SubscribeLocalEvent<DidUnequipEvent>(OnDidUnequip);
    }

    private void OnClothingGotEquipped(Entity<GhostRoleOnEquipTriggerComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!TryComp<GhostRoleOnTriggerComponent>(ent, out var trigger))
            return;

        _makeGhostRole.TryMakeOnTrigger(args.Wearer, trigger);
    }

    private void OnClothingGotUnequipped(Entity<GhostRoleOnEquipTriggerComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        _makeGhostRole.CleanupGhostRole(args.Wearer);
    }

    private void OnDidUnequip(DidUnequipEvent args)
    {
        if (!HasComp<GhostRoleOnEquipTriggerComponent>(args.Equipment))
            return;

        _makeGhostRole.CleanupGhostRole(args.Equipee);
    }
}
