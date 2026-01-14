using System.Collections.Concurrent;
using System.Net;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

using Content.Shared.Connection.IPBlocking;

namespace Content.Server.Connection.IPBlocking;

/// <summary>
/// Система блокировки IP-адресов для защиты от перегрузки памяти
/// при получении подозрительных запросов с некорректными длинами ответов.
/// </summary>
public sealed class IPBlockingSystem : IIPBlockingSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private readonly ConcurrentDictionary<IPAddress, DateTime> _blockedIPs = new();
    private ISawmill _sawmill = default!;

    private bool _enabled;
    private int _blockDurationSeconds;
    private int _maxResponseLength;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("ipblocking");

        _cfg.OnValueChanged(CCVars.GameIPBlockingEnabled, b => _enabled = b, true);
        _cfg.OnValueChanged(CCVars.GameIPBlockingDuration, b => _blockDurationSeconds = b, true);
        _cfg.OnValueChanged(CCVars.GameIPBlockingMaxResponseLength, b => _maxResponseLength = b, true);
    }

    /// <summary>
    /// Проверяет, заблокирован ли указанный IP-адрес.
    /// </summary>
    public bool IsBlocked(IPAddress ip)
    {
        if (!_enabled)
            return false;

        if (!_blockedIPs.TryGetValue(ip, out var unblockTime))
            return false;

        // Проверяем, не истекла ли блокировка
        if (DateTime.UtcNow >= unblockTime)
        {
            _blockedIPs.TryRemove(ip, out _);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Блокирует IP-адрес на указанное время с указанной причиной.
    /// </summary>
    public void BlockIP(IPAddress ip, TimeSpan duration, string reason)
    {
        if (!_enabled)
            return;

        var unblockTime = DateTime.UtcNow + duration;
        _blockedIPs.AddOrUpdate(ip, unblockTime, (_, _) => unblockTime);

        _sawmill.Warning($"Заблокирован IP {ip} на {duration.TotalMinutes:F1} минут. Причина: {reason}");
    }

    /// <summary>
    /// Блокирует IP-адрес на время, указанное в CVar, с указанной причиной.
    /// </summary>
    public void BlockIP(IPAddress ip, string reason)
    {
        var duration = TimeSpan.FromSeconds(_blockDurationSeconds);
        BlockIP(ip, duration, reason);
    }

    /// <summary>
    /// Проверяет длину ответа и блокирует IP при обнаружении подозрительного значения.
    /// </summary>
    /// <returns>true, если длина подозрительная и IP был заблокирован</returns>
    public bool CheckAndBlockSuspiciousLength(IPAddress ip, int length, string context)
    {
        if (!_enabled)
        {
            _sawmill.Debug($"IP blocking is disabled, skipping block for {ip}");
            return false;
        }

        // Проверяем на отрицательные или слишком большие значения
        if (length < 0 || length > _maxResponseLength)
        {
            var reason = $"Подозрительная длина ответа: {length} байт (контекст: {context})";
            _sawmill.Info($"Blocking IP {ip} for suspicious length {length} in context {context}");
            BlockIP(ip, reason);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Очищает истекшие блокировки. Должен вызываться периодически.
    /// </summary>
    public void Update()
    {
        if (!_enabled)
            return;

        var now = DateTime.UtcNow;
        var keysToRemove = new List<IPAddress>();

        foreach (var (ip, unblockTime) in _blockedIPs)
        {
            if (now >= unblockTime)
            {
                keysToRemove.Add(ip);
            }
        }

        foreach (var ip in keysToRemove)
        {
            _blockedIPs.TryRemove(ip, out _);
        }
    }

    /// <summary>
    /// Разблокирует IP-адрес вручную.
    /// </summary>
    public void UnblockIP(IPAddress ip)
    {
        if (_blockedIPs.TryRemove(ip, out _))
        {
            _sawmill.Info($"IP {ip} разблокирован вручную");
        }
    }
}

