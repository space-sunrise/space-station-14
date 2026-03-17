using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise;

public sealed class InfiltrationSuitSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> InfiltrationSuitTag = "InfiltrationSuit";

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

        TryUncloakSuit(ent.Owner);
    }

    private void OnMeleeAttack(Entity<InventoryComponent> ent, ref MeleeAttackEvent args)
    {
        TryUncloakSuit(ent.Owner);
    }

    private void OnShoot(Entity<GunComponent> ent, ref OnNonEmptyGunShotEvent args)
    {
        TryUncloakSuit(args.User);
    }

    private void TryUncloakSuit(EntityUid uid)
    {
        if (!TryComp<InventoryComponent>(uid, out var inventory))
            return;

        if (!_inventory.TryGetSlotEntity(uid, "outerClothing", out var suit, inventory))
            return;

        if (!_tag.HasTag(suit.Value, InfiltrationSuitTag))
            return;

        _toggle.TryDeactivate(suit.Value, uid);
    }
}
