using Content.Shared.Inventory.Events;
using Content.Shared.Tag;
using Content.Shared._Sunrise.Inventory.Components;

namespace Content.Shared._Sunrise.Inventory.Systems;

public sealed class ArmorTagRestrictionSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorTagRestrictionComponent, BeingEquippedAttemptEvent>(OnEquippedAttempt);
    }

    private void OnEquippedAttempt(Entity<ArmorTagRestrictionComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (ent.Comp.RequiredTag != null && !_tagSystem.HasTag(args.EquipTarget, ent.Comp.RequiredTag.Value))
            args.Cancel();
    }
}
