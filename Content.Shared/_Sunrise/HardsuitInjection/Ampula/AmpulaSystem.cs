using Content.Shared.Interaction;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared._Sunrise.HardsuitInjection.Components;

namespace Content.Shared._Sunrise.HardsuitInjection.EntitySystems;

public sealed partial class AmpulaSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<AmpulaComponent, AfterInteractEvent>(OnAfterInteract);
    }
    private void OnAfterInteract(EntityUid uid, AmpulaComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;
        if (args.Handled)
            return;
        var target = args.Target;
        var user = args.User;
        var ampula = args.Used;
        var sys = _entManager.System<ItemSlotsSystem>();
        if (uid != ampula)
            return;
        if (!TryComp<InventoryComponent>(target, out var inventory))
            return;
        if (!_entManager.System<InventorySystem>().TryGetSlotEntity(target.Value, "outerClothing", out var slot, inventory))
            return;
        if (!TryComp<ItemSlotsComponent>(slot, out var itemslots))
            return;
        if (!TryComp<InjectComponent>(slot, out var containerlock))
            return;
    }

}
