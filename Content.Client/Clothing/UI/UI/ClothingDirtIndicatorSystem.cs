using Content.Client.Clothing.Dirt.UI;
using Content.Shared.Clothing.Dirt;
using Content.Shared.Inventory.Events;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Clothing.Dirt.UI;

public sealed class ClothingDirtIndicatorSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingDirtComponent, ComponentHandleState>(OnState);
        SubscribeLocalEvent<ClothingDirtComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ClothingDirtComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<ClothingDirtReceiverComponent, DidEquipEvent>(OnEquip);
        SubscribeLocalEvent<ClothingDirtReceiverComponent, DidUnequipEvent>(OnUnequip);
    }

    private void OnState(EntityUid uid, ClothingDirtComponent _, ref ComponentHandleState __)
        => Push(uid);

    private void OnInit(EntityUid uid, ClothingDirtComponent _, ComponentInit __)
        => Push(uid);

    private void OnRemove(EntityUid uid, ClothingDirtComponent _, ComponentRemove __)
        => Push(uid); // Refresh() сама скроет если компонента нет

    private void OnEquip(EntityUid _, ClothingDirtReceiverComponent __, DidEquipEvent args)
        => Push(args.Equipment);

    private void OnUnequip(EntityUid _, ClothingDirtReceiverComponent __, DidUnequipEvent args)
        => Push(args.Equipment);

    private void Push(EntityUid item)
    {
        foreach (var root in _ui.AllRoots)
        {
            foreach (var ctrl in root.FindAllControlsOfType<ClothingDirtInventorySlotControl>())
            {
                if (ctrl.HeldEntity == item)
                    ctrl.Refresh();
            }
        }
    }
}
