using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Stealth;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise;

public sealed class InfiltrationSuitSystem : EntitySystem
{
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> InfiltrationSuitTag = "InfiltrationSuit";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageChangedEvent>(OnDamageTaken);
        SubscribeLocalEvent<AttemptMeleeEvent>(OnMeleeAttack);
        SubscribeLocalEvent<OnNonEmptyGunShotEvent>(OnShoot);
    }

    private void OnDamageTaken(ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        TryUncloakSuit(args.Damageable.Owner);
    }

    private void OnMeleeAttack(ref AttemptMeleeEvent args)
    {
        if (args.Cancelled)
            return;

        TryUncloakSuit(args.User);
    }

    private void OnShoot(ref OnNonEmptyGunShotEvent args)
    {
        TryUncloakSuit(args.User);
    }

    private void TryUncloakSuit(EntityUid wearer)
    {
        if (!TryComp<InventoryComponent>(wearer, out var inventory))
            return;

        if (!_inventory.TryGetSlotEntity(wearer, "outerClothing", out var suit, inventory))
            return;

        if (!_tag.HasTag(suit.Value, InfiltrationSuitTag))
            return;

        _stealth.SetEnabled(suit.Value, false);
    }
}
