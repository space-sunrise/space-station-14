using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CartridgeLoader.Cartridges;

/// <summary>
/// Событие сообщения UI фото-картриджа
/// </summary>
[Serializable, NetSerializable]
public sealed class PhotoUiMessageEvent : CartridgeMessageEvent
{
    public readonly PhotoUiAction Action;
    public readonly string? PhotoId;
    public readonly string? RecipientId;
    public readonly string? GroupId;

    public PhotoUiMessageEvent(
        PhotoUiAction action,
        string? photoId = null,
        string? recipientId = null,
        string? groupId = null,
        bool? flashEnabled = null)
    {
        Action = action;
        PhotoId = photoId;
        RecipientId = recipientId;
        GroupId = groupId;
        FlashEnabled = flashEnabled;
    }

    public bool? FlashEnabled { get; }
}

/// <summary>
/// Действия UI фото-картриджа
/// </summary>
[Serializable, NetSerializable]
public enum PhotoUiAction
{
    CapturePhoto,
    SendPhotoToMessenger,
    RequestGallery,
    DeletePhoto,
    ToggleFlash
}
