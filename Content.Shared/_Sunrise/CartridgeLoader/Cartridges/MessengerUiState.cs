using Content.Shared._Sunrise.Messenger;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CartridgeLoader.Cartridges;

/// <summary>
/// Состояние UI мессенджера
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerUiState : BoundUserInterfaceState
{
    /// <summary>
    /// Зарегистрирован ли пользователь
    /// </summary>
    public bool IsRegistered { get; }

    /// <summary>
    /// Доступен ли сервер
    /// </summary>
    public bool ServerAvailable { get; }

    /// <summary>
    /// ID текущего пользователя
    /// </summary>
    public string? CurrentUserId { get; }

    /// <summary>
    /// Список пользователей
    /// </summary>
    public List<MessengerUser> Users { get; }

    /// <summary>
    /// Список групп
    /// </summary>
    public List<MessengerGroup> Groups { get; }

    /// <summary>
    /// История сообщений по чатам
    /// </summary>
    public Dictionary<string, List<MessengerMessage>> MessageHistory { get; }

    /// <summary>
    /// Заглушенные личные чаты (chatId)
    /// </summary>
    public HashSet<string> MutedPersonalChats { get; }

    /// <summary>
    /// Заглушенные групповые чаты (groupId)
    /// </summary>
    public HashSet<string> MutedGroupChats { get; }

    /// <summary>
    /// Количество непрочитанных сообщений по чатам (chatId -> количество)
    /// </summary>
    public Dictionary<string, int> UnreadCounts { get; }

    /// <summary>
    /// Активные приглашения в группы
    /// </summary>
    public List<MessengerGroupInvite> ActiveInvites { get; }

    /// <summary>
    /// Закрепленные чаты (chatId)
    /// </summary>
    public HashSet<string> PinnedChats { get; }

    /// <summary>
    /// Галерея фотографий для выбора (опционально)
    /// </summary>
    public Dictionary<string, PhotoMetadata>? PhotoGallery { get; }

    public MessengerUiState(
        bool isRegistered,
        bool serverAvailable,
        string? currentUserId,
        List<MessengerUser> users,
        List<MessengerGroup> groups,
        Dictionary<string, List<MessengerMessage>> messageHistory,
        HashSet<string> mutedPersonalChats,
        HashSet<string> mutedGroupChats,
        Dictionary<string, int> unreadCounts,
        List<MessengerGroupInvite> activeInvites,
        HashSet<string> pinnedChats,
        Dictionary<string, PhotoMetadata>? photoGallery = null)
    {
        IsRegistered = isRegistered;
        ServerAvailable = serverAvailable;
        CurrentUserId = currentUserId;
        Users = users;
        Groups = groups;
        MessageHistory = messageHistory;
        MutedPersonalChats = mutedPersonalChats;
        MutedGroupChats = mutedGroupChats;
        UnreadCounts = unreadCounts;
        ActiveInvites = activeInvites;
        PinnedChats = pinnedChats;
        PhotoGallery = photoGallery;
    }
}
