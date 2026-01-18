using System.Collections.Concurrent;
using System.Net;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

using Content.Shared.Connection.IPBlocking;

namespace Content.Server.Connection.IPBlocking;

public sealed class IPBlockingSystem : IIPBlockingSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private readonly ConcurrentDictionary<IPAddress, DateTime> _blockedIPs = new();
    private readonly ConcurrentDictionary<IPAddress, object> _unhandledMessageLocks = new();
    private readonly ConcurrentDictionary<IPAddress, List<DateTime>> _unhandledMessageTimestamps = new();
    private ISawmill _sawmill = default!;

    private bool _enabled;
    private int _blockDurationSeconds;
    private int _maxResponseLength;
    private int _unhandledMessageRateLimit;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("ipblocking");

        _cfg.OnValueChanged(SunriseCCVars.GameIPBlockingEnabled, b => _enabled = b, true);
        _cfg.OnValueChanged(SunriseCCVars.GameIPBlockingDuration, b => _blockDurationSeconds = b, true);
        _cfg.OnValueChanged(SunriseCCVars.GameIPBlockingMaxResponseLength, b => _maxResponseLength = b, true);
        _cfg.OnValueChanged(SunriseCCVars.GameIPBlockingUnhandledMessageRateLimit, b => _unhandledMessageRateLimit = b, true);
    }

    public bool IsBlocked(IPAddress ip)
    {
        if (!_enabled)
            return false;

        if (!_blockedIPs.TryGetValue(ip, out var unblockTime))
            return false;

        if (DateTime.UtcNow >= unblockTime)
        {
            _blockedIPs.TryRemove(ip, out _);
            return false;
        }

        return true;
    }

    public void BlockIP(IPAddress ip, TimeSpan duration, string reason)
    {
        if (!_enabled)
            return;

        var unblockTime = DateTime.UtcNow + duration;
        _blockedIPs.AddOrUpdate(ip, unblockTime, (_, _) => unblockTime);

        _sawmill.Warning($"Заблокирован IP {ip} на {duration.TotalMinutes:F1} минут. Причина: {reason}");
    }

    public void BlockIP(IPAddress ip, string reason)
    {
        var duration = TimeSpan.FromSeconds(_blockDurationSeconds);
        BlockIP(ip, duration, reason);
    }

    public bool CheckAndBlockSuspiciousLength(IPAddress ip, int length, string context)
    {
        if (!_enabled)
        {
            _sawmill.Debug($"IP blocking is disabled, skipping block for {ip}");
            return false;
        }

        if (length < 0 || length > _maxResponseLength)
        {
            var reason = $"Подозрительная длина ответа: {length} байт (контекст: {context})";
            _sawmill.Info($"Blocking IP {ip} for suspicious length {length} in context {context}");
            BlockIP(ip, reason);
            return true;
        }

        return false;
    }

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

    public void UnblockIP(IPAddress ip)
    {
        if (_blockedIPs.TryRemove(ip, out _))
        {
            _sawmill.Info($"IP {ip} unblocked");
        }
    }

    public bool CheckAndBlockUnhandledMessageRate(IPAddress ip, string messageType)
    {
        if (!_enabled)
            return false;

        if (IsBlocked(ip))
            return true;

        var now = DateTime.UtcNow;
        var lockObj = _unhandledMessageLocks.GetOrAdd(ip, _ => new object());
        var timestamps = _unhandledMessageTimestamps.GetOrAdd(ip, _ => new List<DateTime>());

        lock (lockObj)
        {
            timestamps.RemoveAll(t => (now - t).TotalSeconds > 1.0);

            timestamps.Add(now);

            if (timestamps.Count > _unhandledMessageRateLimit)
            {
                var reason = $"Превышен лимит необработанных библиотечных сообщений: {timestamps.Count} сообщений/сек (тип: {messageType})";
                BlockIP(ip, reason);
                _unhandledMessageTimestamps.TryRemove(ip, out _);
                _unhandledMessageLocks.TryRemove(ip, out _);
                return true;
            }
        }

        return false;
    }
}

