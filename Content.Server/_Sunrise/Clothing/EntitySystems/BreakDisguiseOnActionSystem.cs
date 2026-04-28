using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Server.Actions;
using Content.Server._Sunrise.Clothing.Components;

namespace Content.Server._Sunrise.Clothing.EntitySystems;

/// <summary>
/// Deactivates disguising clothing when its wearer is revealed by configured combat actions.
/// </summary>
public sealed class BreakDisguiseOnActionSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BreakDisguiseOnActionComponent, InventoryRelayedEvent<AttackedEvent>>(OnAttacked);
        SubscribeLocalEvent<BreakDisguiseOnActionComponent, InventoryRelayedEvent<MeleeAttackEvent>>(OnMeleeAttack);
        SubscribeLocalEvent<BreakDisguiseOnActionComponent, InventoryRelayedEvent<SelfBeforeGunShotEvent>>(OnBeforeGunShot);
    }

    private void OnAttacked(Entity<BreakDisguiseOnActionComponent> ent, ref InventoryRelayedEvent<AttackedEvent> args)
    {
        if (!ent.Comp.BreakOnAttacked)
            return;

        TryBreakDisguise(ent, args);
    }

    private void OnMeleeAttack(Entity<BreakDisguiseOnActionComponent> ent, ref InventoryRelayedEvent<MeleeAttackEvent> args)
    {
        if (!ent.Comp.BreakOnMeleeAttack)
            return;

        TryBreakDisguise(ent, args);
    }

    private void OnBeforeGunShot(Entity<BreakDisguiseOnActionComponent> ent, ref InventoryRelayedEvent<SelfBeforeGunShotEvent> args)
    {
        if (!ent.Comp.BreakOnGunShot || args.Args.Cancelled)
            return;

        TryBreakDisguise(ent, args);
    }

    private bool TryBreakDisguise(Entity<BreakDisguiseOnActionComponent> ent, EntityUid wearer)
    {
        if (!CanBreakDisguise(ent))
            return false;

        return BreakDisguise(ent, wearer);
    }

    private bool CanBreakDisguise(Entity<BreakDisguiseOnActionComponent> ent)
    {
        return _toggle.IsActivated(ent);
    }

    private bool BreakDisguise(Entity<BreakDisguiseOnActionComponent> ent, EntityUid wearer)
    {
        if (!_toggle.TryDeactivate(ent, wearer, predicted: false))
            return false;

        StartCooldown(ent);
        return true;
    }

    private void StartCooldown(Entity<BreakDisguiseOnActionComponent> ent)
    {
        if (ent.Comp.Cooldown <= TimeSpan.Zero)
            return;

        if (!TryComp<ToggleClothingComponent>(ent, out var toggleClothing) || toggleClothing.ActionEntity == null)
            return;

        _actions.SetIfBiggerCooldown(toggleClothing.ActionEntity.Value, ent.Comp.Cooldown);
    }
}
