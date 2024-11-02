using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._Sunrise.CentCom;

public abstract class SharedCentComConsoleSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<CentComConsoleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CentComConsoleComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentInit(EntityUid uid, CentComConsoleComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, CentComConsoleComponent.IdCardSlotId, component.IdSlot);
    }

    private void OnComponentRemove(EntityUid uid, CentComConsoleComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.IdSlot);
    }
}
