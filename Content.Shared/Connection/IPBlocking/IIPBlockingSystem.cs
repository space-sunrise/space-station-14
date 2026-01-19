using System;
using System.Net;

namespace Content.Shared.Connection.IPBlocking;

public interface IIPBlockingSystem
{
    void Initialize();

    bool IsBlocked(IPAddress ip);

    void BlockIP(IPAddress ip, TimeSpan duration, string reason);

    void BlockIP(IPAddress ip, string reason);

    bool CheckAndBlockSuspiciousLength(IPAddress ip, int length, string context);

    bool CheckAndBlockUnhandledMessageRate(IPAddress ip, string messageType);

    void Update();
}

