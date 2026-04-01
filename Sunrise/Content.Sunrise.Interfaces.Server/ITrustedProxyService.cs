using Robust.Shared.Network;
using System.Net;

namespace Content.Sunrise.Interfaces.Server
{
    public interface ITrustedProxyService
    {
        void Initialize();
        void Update();
        bool TryGetRealRemoteEndPoint(NetUserId user, out IPEndPoint? realEndPoint);
    }
}
