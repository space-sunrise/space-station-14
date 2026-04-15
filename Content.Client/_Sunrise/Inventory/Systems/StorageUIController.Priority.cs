using Content.Client.UserInterface.Systems.Storage;
using Content.Client.UserInterface.Systems.Storage.Controls;
using Content.Shared._Sunrise.Inventory.Events;
using Content.Shared.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

#pragma warning disable IDE0130
namespace Content.Client.UserInterface.Systems.Storage;

// Sunrise: Storage priority extension
public sealed partial class StorageUIController
{
    public void OnPiecePressedPriority(GUIBoundKeyEventArgs args, StorageWindow window, ItemGridPiece control)
    {
        if (args.Function == ContentKeyFunctions.ToggleItemPriority)
        {
            if (window.StorageEntity is not { } storage)
                return;

            EntityManager.RaisePredictiveEvent(new StorageToggleItemPriorityEvent(
                EntityManager.GetNetEntity(control.Entity),
                EntityManager.GetNetEntity(storage)));
            args.Handle();
        }
    }
}
