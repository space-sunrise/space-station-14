using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Messenger;

/// <summary>
/// Информация о пользователе мессенджера
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerUser
{
    /// <summary>
    /// Уникальный ID пользователя (адрес DeviceNetwork КПК)
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Имя пользователя
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Должность пользователя (опционально)
    /// </summary>
    public string? JobTitle { get; set; }

    /// <summary>
    /// ID отдела пользователя (опционально)
    /// </summary>
    public string? DepartmentId { get; set; }

    /// <summary>
    /// ID иконки роли пользователя (опционально)
    /// </summary>
    public ProtoId<JobIconPrototype>? JobIconId { get; set; }

    public MessengerUser(string userId, string name, string? jobTitle = null, string? departmentId = null, ProtoId<JobIconPrototype>? jobIconId = null)
    {
        UserId = userId;
        Name = name;
        JobTitle = jobTitle;
        DepartmentId = departmentId;
        JobIconId = jobIconId;
    }
}
