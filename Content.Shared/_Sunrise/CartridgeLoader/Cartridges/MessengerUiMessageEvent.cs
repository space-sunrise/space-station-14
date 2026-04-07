using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CartridgeLoader.Cartridges;

/// <summary>
/// Событие сообщения UI мессенджера
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerUiMessageEvent : CartridgeMessageEvent
{
    public readonly MessengerUiAction Action;
    public readonly string? RecipientId;
    public readonly string? GroupId;
    public readonly string? Content;
    public readonly string? GroupName;
    public readonly string? UserId;
    public readonly string? ChatId;
    public readonly bool? IsMuted;
    public readonly long? MessageId;
    public readonly string? ImagePath;

    public MessengerUiMessageEvent(
        MessengerUiAction action,
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
        Action = action;
        RecipientId = recipientId;
        GroupId = groupId;
        Content = content;
        GroupName = groupName;
        UserId = userId;
        ChatId = chatId;
        IsMuted = isMuted;
        MessageId = messageId;
        ImagePath = imagePath;
    }
}

/// <summary>
/// Действия UI мессенджера
/// </summary>
[Serializable, NetSerializable]
public enum MessengerUiAction
{
    SendMessage,
    CreateGroup,
    AddToGroup,
    RemoveFromGroup,
    RequestUsers,
    RequestGroups,
    RequestMessages,
    ToggleMute,
    AcceptInvite,
    DeclineInvite,
    LeaveGroup,
    DeleteMessage,
    TogglePin,
    RequestPhotos
}
