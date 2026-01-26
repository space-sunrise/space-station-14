using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Messenger;

/// <summary>
/// Тип группы в мессенджере
/// </summary>
[Serializable, NetSerializable]
public enum MessengerGroupType
{
    /// <summary>
    /// Пользовательская группа, созданная игроком
    /// </summary>
    UserCreated,

    /// <summary>
    /// Автоматическая группа (департамент, общий чат и т.д.)
    /// </summary>
    Automatic
}
