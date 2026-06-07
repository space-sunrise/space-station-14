using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Components.Abilities;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Interaction.Components;
using Content.Shared.Humanoid;
using Content.Shared.Body.Components;
using Content.Shared.Wieldable;
using Content.Shared.Body.Systems;

namespace Content.Server._Sunrise.Antags.Vampires.Systems.Abilities;

/// <summary>
/// Handles vampiric claws lifecycle and effects
/// </summary>
public sealed class VampiricClawsSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly VampireSystem _vampire = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VampiricClawsComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<VampiricClawsComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<VampiricClawsComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<VampiricClawsComponent, ItemUnwieldedEvent>(OnUnwielded);
    }

    private void OnInit(Entity<VampiricClawsComponent> ent, ref MapInitEvent args) => EnsureComp<UnremoveableComponent>(ent);

    private void OnUseInHand(Entity<VampiricClawsComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (TryComp<VampireComponent>(args.User, out var vamp))
            ClearClawsReference(args.User, ent.Owner, vamp);

        _popup.PopupEntity(Loc.GetString("vampiric-claws-remove-popup"), ent.Owner, args.User);

        QueueDel(ent);
    }

    private void OnMeleeHit(Entity<VampiricClawsComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (!TryComp<VampireComponent>(args.User, out var vamp))
            return;

        var bloodGained = 0;
        foreach (var hitEntity in args.HitEntities)
        {
            if (HasComp<HumanoidAppearanceComponent>(hitEntity)
                && TryComp<BloodstreamComponent>(hitEntity, out var victimBlood)
                && _bloodstream.TryModifyBloodLevel((hitEntity, victimBlood), -ent.Comp.BloodPerHit))
            {
                bloodGained += ent.Comp.BloodPerHit;
                ent.Comp.HitsRemaining--;
                _vampire.AddBlood((args.User, vamp), ent.Comp.BloodPerHit, hitEntity);

                if (ent.Comp.HitsRemaining <= 0)
                    break;
            }
        }

        if (bloodGained > 0)
        {
            Dirty(ent);
            if (ent.Comp.HitsRemaining <= 0)
            {
                ClearClawsReference(args.User, ent.Owner, vamp);
                QueueDel(ent);
            }
        }
    }

    private void ClearClawsReference(EntityUid user, EntityUid claws, VampireComponent vampire)
    {
        if (vampire.SpawnedClaws != claws)
            return;

        vampire.SpawnedClaws = null;
        Dirty(user, vampire);
    }

    private void OnUnwielded(Entity<VampiricClawsComponent> ent, ref ItemUnwieldedEvent args)
    {
        if (TryComp<VampireComponent>(args.User, out var vampire) && vampire.SpawnedClaws == ent.Owner)
        {
            vampire.SpawnedClaws = null;
            Dirty(args.User, vampire);
        }

        QueueDel(ent);
    }
}
