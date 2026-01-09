using System.Net;

namespace Content.Shared.Connection.IPBlocking;

/// <summary>
/// Интерфейс для системы блокировки IP-адресов.
/// </summary>
public interface IIPBlockingSystem
{
    /// <summary>
    /// Инициализирует систему блокировки IP.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Проверяет, заблокирован ли указанный IP-адрес.
    /// </summary>
    bool IsBlocked(IPAddress ip);

    /// <summary>
    /// Проверяет длину ответа и блокирует IP при обнаружении подозрительного значения.
    /// </summary>
    /// <returns>true, если длина подозрительная и IP был заблокирован</returns>
    bool CheckAndBlockSuspiciousLength(IPAddress ip, int length, string context);

    /// <summary>
    /// Очищает истекшие блокировки. Должен вызываться периодически.
    /// </summary>
    void Update();
}

