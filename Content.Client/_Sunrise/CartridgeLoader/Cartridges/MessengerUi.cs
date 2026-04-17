using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared._Sunrise.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.CartridgeLoader.Cartridges;

public sealed partial class MessengerUi : UIFragment
{
    private MessengerUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new MessengerUiFragment();
        _fragment.OnSendMessage += (recipientId, groupId, content, imagePath) =>
            SendMessengerMessage(MessengerUiAction.SendMessage, userInterface, recipientId: recipientId, groupId: groupId, content: content, imagePath: imagePath);
        _fragment.OnCreateGroup += (groupName) =>
            SendMessengerMessage(MessengerUiAction.CreateGroup, userInterface, groupName: groupName);
        _fragment.OnAddToGroup += (groupId, userId) =>
            SendMessengerMessage(MessengerUiAction.AddToGroup, userInterface, groupId: groupId, userId: userId);
        _fragment.OnRemoveFromGroup += (groupId, userId) =>
            SendMessengerMessage(MessengerUiAction.RemoveFromGroup, userInterface, groupId: groupId, userId: userId);
        _fragment.OnRequestMessages += (chatId) =>
            SendMessengerMessage(MessengerUiAction.RequestMessages, userInterface, chatId: chatId);
        _fragment.OnToggleMute += (chatId, isMuted) =>
            SendMessengerMessage(MessengerUiAction.ToggleMute, userInterface, chatId: chatId, isMuted: isMuted);
        _fragment.OnAcceptInvite += (groupId) =>
            SendMessengerMessage(MessengerUiAction.AcceptInvite, userInterface, groupId: groupId);
        _fragment.OnDeclineInvite += (groupId) =>
            SendMessengerMessage(MessengerUiAction.DeclineInvite, userInterface, groupId: groupId);
        _fragment.OnLeaveGroup += (groupId) =>
            SendMessengerMessage(MessengerUiAction.LeaveGroup, userInterface, groupId: groupId);
        _fragment.OnDeleteMessage += (chatId, messageId) =>
            SendMessengerMessage(MessengerUiAction.DeleteMessage, userInterface, chatId: chatId, messageId: messageId);
        _fragment.OnTogglePin += (chatId) =>
            SendMessengerMessage(MessengerUiAction.TogglePin, userInterface, chatId: chatId);
        _fragment.OnRequestPhotos += () =>
            SendMessengerMessage(MessengerUiAction.RequestPhotos, userInterface);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MessengerUiState messengerState)
            return;

        _fragment?.UpdateState(messengerState);
    }

    private void SendMessengerMessage(
        MessengerUiAction action,
        BoundUserInterface userInterface,
        string? recipientId = null,
        string? groupId = null,
        string? content = null,
        string? groupName = null,
        string? userId = null,
        string? chatId = null,
        bool? isMuted = null,
        long? messageId = null,
        string? imagePath = null)
    {
        var messengerMessage = new MessengerUiMessageEvent(action, recipientId, groupId, content, groupName, userId, chatId, isMuted, messageId, imagePath);
        var message = new CartridgeUiMessage(messengerMessage);
        userInterface.SendMessage(message);
    }
}
