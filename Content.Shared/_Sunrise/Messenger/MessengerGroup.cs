using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Messenger;

/// <summary>
/// Информация о группе в мессенджере
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerGroup
{
    /// <summary>
    /// Уникальный ID группы
    /// </summary>
    public string GroupId { get; set; }

    /// <summary>
    /// Название группы
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// ID участников группы
    /// </summary>
    public HashSet<string> Members { get; set; }

    /// <summary>
    /// Тип группы
    /// </summary>
    public MessengerGroupType Type { get; set; }

    /// <summary>
    /// ID прототипа автоматической группы (null для пользовательских групп)
    /// </summary>
    public string? AutoGroupPrototypeId { get; set; }

    /// <summary>
    /// ID владельца группы (создателя). null для автоматических групп
    /// </summary>
    public string? OwnerId { get; set; }

    public MessengerGroup(string groupId, string name, HashSet<string> members, MessengerGroupType type = MessengerGroupType.UserCreated, string? autoGroupPrototypeId = null, string? ownerId = null)
    {
        GroupId = groupId;
        Name = name;
        Members = members;
        Type = type;
        AutoGroupPrototypeId = autoGroupPrototypeId;
        OwnerId = ownerId;
    }
}
