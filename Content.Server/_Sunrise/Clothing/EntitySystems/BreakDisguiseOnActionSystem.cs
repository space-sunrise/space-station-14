using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Server.Actions;
using Content.Server._Sunrise.Clothing.Components;

namespace Content.Server._Sunrise.Clothing.EntitySystems;

/// <summary>
/// Deactivates equipped disguising clothing when the wearer takes damage, attacks, or shoots.
/// </summary>
public sealed class BreakDisguiseOnActionSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, DamageChangedEvent>(OnDamageTaken);
        SubscribeLocalEvent<InventoryComponent, MeleeAttackEvent>(OnMeleeAttack);
        SubscribeLocalEvent<GunComponent, OnNonEmptyGunShotEvent>(OnShoot);
    }

    private void OnDamageTaken(Entity<InventoryComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        BreakWornDisguises(ent);
    }

    private void OnMeleeAttack(Entity<InventoryComponent> ent, ref MeleeAttackEvent args)
    {
        BreakWornDisguises(ent);
    }

    private void OnShoot(Entity<GunComponent> ent, ref OnNonEmptyGunShotEvent args)
    {
        if (!TryComp<InventoryComponent>(args.User, out var inventory))
            return;

        BreakWornDisguises((args.User, inventory));
    }

    private void BreakWornDisguises(Entity<InventoryComponent> ent)
    {
        var enumerator = _inventory.GetSlotEnumerator((ent.Owner, ent.Comp));
        while (enumerator.NextItem(out var item))
        {
            if (!TryComp<BreakDisguiseOnActionComponent>(item, out var disguise))
                continue;

            if (!_toggle.IsActivated(item))
                continue;

            if (!_toggle.TryDeactivate(item, ent.Owner, predicted: false))
                continue;

            StartCooldown((item, disguise));
        }
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
