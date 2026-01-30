using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Messenger;

/// <summary>
/// Приглашение в группу мессенджера
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerGroupInvite
{
    /// <summary>
    /// ID группы, в которую приглашают
    /// </summary>
    public string GroupId { get; set; }

    /// <summary>
    /// Название группы
    /// </summary>
    public string GroupName { get; set; }

    /// <summary>
    /// ID пользователя, который отправил приглашение
    /// </summary>
    public string InviterId { get; set; }

    /// <summary>
    /// Имя пользователя, который отправил приглашение
    /// </summary>
    public string InviterName { get; set; }

    /// <summary>
    /// ID пользователя, которому отправлено приглашение
    /// </summary>
    public string InviteeId { get; set; }

    /// <summary>
    /// Время создания приглашения
    /// </summary>
    public TimeSpan CreatedAt { get; set; }

    public MessengerGroupInvite(string groupId, string groupName, string inviterId, string inviterName, string inviteeId, TimeSpan createdAt)
    {
        GroupId = groupId;
        GroupName = groupName;
        InviterId = inviterId;
        InviterName = inviterName;
        InviteeId = inviteeId;
        CreatedAt = createdAt;
    }
}
