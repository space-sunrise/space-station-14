using Content.Shared.Hands.EntitySystems;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Light.Components;
using Content.Shared.Clothing.Components;
using Content.Shared._Sunrise.PersonalBiocode;
using Content.Shared.Inventory.Events;
using Content.Shared.Forensics.Components;
using Content.Shared._Sunrise.Modsuit;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.Clothing.EntitySystems;

namespace Content.Server._Sunrise.Modsuit;

public sealed class ModsuitSystem : SharedModsuitSystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModsuitComponent, ContainerIsInsertingAttemptEvent>(OnSuitInsertAttempt);
        SubscribeLocalEvent<ModsuitComponent, GotEquippedEvent>(OnEquip);
    }

    private void OnSuitInsertAttempt(EntityUid uid, ModsuitComponent comp, ContainerIsInsertingAttemptEvent args)
    {

        if (comp.IsActivated == true)
            return;
        
        if (args.Container.ID != "modsuit_core") 
            return;

        if (!TryComp<TagComponent>(args.EntityUid, out var itemSlots) || !_tag.HasTag(itemSlots, "ModsuitCore"))
            return;
        
        comp.IsActivated = true;
        EntityManager.Dirty(uid, comp);

    }

    public void OnEquip(EntityUid uid, ModsuitComponent comp, GotEquippedEvent args)
    {
        if (comp.RoundStartBiocode == true)
        {
            if (!TryComp<DnaComponent>(args.Equipee, out var PersonDNA))
                return;

            if (!TryComp(uid, out ToggleableClothingComponent? Toggleable))
                return;

            if (!TryComp<PersonalBiocodeComponent>(Toggleable.ClothingUid, out var SuitBiocode))
                return;
            
            if (PersonDNA.DNA != null)
                SuitBiocode.DNA = PersonDNA.DNA;

            SuitBiocode.IsAuthorized = true;
            EntityManager.Dirty(SuitBiocode.Owner, SuitBiocode);

            comp.RoundStartBiocode = false;
            EntityManager.Dirty(uid, comp);
        }

    }
}