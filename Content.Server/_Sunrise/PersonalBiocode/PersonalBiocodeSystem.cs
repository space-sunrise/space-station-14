using Content.Shared.NPC.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Server._Sunrise.PersonalBiocode;
using Content.Shared._Sunrise.PersonalBiocode;
using Content.Shared.Emag.Systems;
﻿using Content.Shared.Actions;
using Content.Shared.Forensics.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Clothing.EntitySystems;

namespace Content.Server._Sunrise.PersonalBiocode;

public sealed class PersonalBiocodeSystem : SharedPersonalBiocodeSystem // Пока только для модсьюитов
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private static readonly EntProtoId Action = "ActionSaveDNA";

    public override void Initialize()
    {
        SubscribeLocalEvent<PersonalBiocodeComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<PersonalBiocodeComponent, StoreDNAActionEvent>(OnDNAStored);
        SubscribeLocalEvent<PersonalBiocodeComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<PersonalBiocodeComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnGetActions(EntityUid uid, PersonalBiocodeComponent comp, GetItemActionsEvent args)
    {
        if (comp.IsAuthorized == false)
        {
            args.AddAction(ref comp.ActionEntity, Action);
        }
    }

    public void OnDNAStored(EntityUid uid, PersonalBiocodeComponent comp, StoreDNAActionEvent args)
    {
        if (args.Handled)
            return;

        if (comp.IsAuthorized == false)
        {
            if (TryComp<DnaComponent>(args.Performer, out var PersonDNA) && PersonDNA.DNA != null)
            {
                comp.DNA = PersonDNA.DNA;
                comp.IsAuthorized = true;
                EntityManager.Dirty(uid, comp);

                _popupSystem.PopupEntity(Loc.GetString("person-dna-was-stored"), args.Performer, args.Performer);
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("person-dna-not-presented"), args.Performer, args.Performer);
            }

        }
        args.Handled = true;
    }

     public void OnEquip(EntityUid uid, PersonalBiocodeComponent comp, GotEquippedEvent args)
    {
        if (comp.IsAuthorized == true)
        {
            if (TryComp(args.Equipee, out DnaComponent? PersonNDA) && comp.DNA == PersonNDA.DNA)
            {
                _popupSystem.PopupClient("biocode-equip-failure", args.Equipee, args.Equipee, PopupType.MediumCaution);     
                return;    
            }

            _inventory.TryUnequip(args.Equipee, "outerClothing", true, true);
        }

    }

    public void OnEmagged(EntityUid uid, PersonalBiocodeComponent comp, GotEmaggedEvent args)
    {
        if (args.Handled)
            return;
        
        if (!comp.BreakAble)
            return;

        EntityManager.RemoveComponent<PersonalBiocodeComponent>(uid);

        //_popupSystem.PopupEntity(Loc.GetString("hardsuit-identification-on-emagged"), uid);

        args.Handled = true;
    }
}
