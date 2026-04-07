using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Messenger;

/// <summary>
/// Сообщение в мессенджере
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerMessage
{
    /// <summary>
    /// ID отправителя
    /// </summary>
    public string SenderId { get; set; }

    /// <summary>
    /// Имя отправителя
    /// </summary>
    public string SenderName { get; set; }

    /// <summary>
    /// Текст сообщения
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Время отправки (относительно начала раунда)
    /// </summary>
    public TimeSpan Timestamp { get; set; }

    /// <summary>
    /// ID группы (null для личных сообщений)
    /// </summary>
    public string? GroupId { get; set; }

    /// <summary>
    /// ID получателя (null для групповых сообщений)
    /// </summary>
    public string? RecipientId { get; set; }

    /// <summary>
    /// Прочитано ли сообщение получателем (только для личных сообщений)
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Уникальный идентификатор сообщения
    /// </summary>
    public long MessageId { get; set; }

    /// <summary>
    /// ID иконки роли отправителя (опционально)
    /// </summary>
    public ProtoId<JobIconPrototype>? SenderJobIconId { get; set; }

    /// <summary>
    /// Путь к сетевому изображению, если сообщение содержит картинку.
    /// Например: "/NetTextures/Messenger/photo_123.png"
    /// </summary>
    public string? ImagePath { get; set; }

    public MessengerMessage(
        string senderId,
        string senderName,
        string content,
        TimeSpan timestamp,
        string? groupId = null,
        string? recipientId = null,
        bool isRead = false,
        long messageId = 0,
        ProtoId<JobIconPrototype>? senderJobIconId = null,
        string? imagePath = null)
    {
        SenderId = senderId;
        SenderName = senderName;
        Content = content;
        Timestamp = timestamp;
        GroupId = groupId;
        RecipientId = recipientId;
        IsRead = isRead;
        MessageId = messageId;
        SenderJobIconId = senderJobIconId;
        ImagePath = imagePath;
    }
}
