using Content.Shared.DeviceNetwork;
using Content.Shared.Messenger;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Messenger;

/// <summary>
/// Компонент сервера мессенджера, который обрабатывает сообщения между КПК
/// </summary>
[RegisterComponent]
[Access(typeof(MessengerServerSystem))]
public sealed partial class MessengerServerComponent : Component
{
    /// <summary>
    /// Словарь зарегистрированных пользователей. Ключ - адрес DeviceNetwork КПК
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<string, MessengerUser> Users = new();

    /// <summary>
    /// Словарь групп. Ключ - ID группы
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<string, MessengerGroup> Groups = new();

    /// <summary>
    /// История сообщений. Ключ - ID чата (userId для личных, groupId для групп)
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<string, List<MessengerMessage>> MessageHistory = new();

    /// <summary>
    /// Максимальное количество сообщений в истории для одного чата
    /// </summary>
    [DataField("maxMessageHistory")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int MaxMessageHistory = 100;

    /// <summary>
    /// Счетчик для генерации уникальных ID групп
    /// </summary>
    [ViewVariables]
    public int GroupIdCounter = 0;

    /// <summary>
    /// Количество непрочитанных сообщений по пользователям и чатам.
    /// Ключ - userId, значение - словарь (chatId -> количество непрочитанных)
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<string, Dictionary<string, int>> UnreadCounts = new();

    /// <summary>
    /// Открытые чаты пользователей. Ключ - userId, значение - chatId открытого чата
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<string, string?> OpenChats = new();

    /// <summary>
    /// Прототип частоты для PDA устройств
    /// </summary>
    [DataField]
    public ProtoId<DeviceFrequencyPrototype> PdaFrequencyId = "PDA";
}
