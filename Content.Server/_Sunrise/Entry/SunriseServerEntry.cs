using System.Linq;
using Robust.Shared.Network;
#if SUNRISE_PRIVATE
using Content.Server._SunrisePrivate.AntiNuke;
using Content.Shared._Sunrise.NetTextures;
using Content.Sunrise.Interfaces.Server;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Utility;
#endif

namespace Content.Server._Sunrise.Entry;

public sealed class SunriseServerEntry
{
    public static void Init()
    {
#if SUNRISE_PRIVATE
        IoCManager.Resolve<ISharedSponsorsManager>().Initialize();
        IoCManager.Resolve<IServerJoinQueueManager>().Initialize();
        IoCManager.Resolve<IServerServiceAuthManager>().Initialize();
        IoCManager.Resolve<AntiNukeManager>().Initialize();
#endif
    }

    public static void PostInit()
    {
        var netManager = IoCManager.Resolve<IServerNetManager>();
        var field = netManager.GetType().GetField("_netPeers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var netPeers = (System.Collections.IEnumerable)field?.GetValue(netManager)!;
        var firstPeerData = netPeers.Cast<object>().FirstOrDefault();

        if (firstPeerData != null)
        {
            var peerField = firstPeerData.GetType().GetField("Peer");
            var lidgrenPeer = (Lidgren.Network.NetPeer)peerField?.GetValue(firstPeerData)!;

            var logMethod = typeof(Lidgren.Network.NetPeer).GetMethod("LogWarning", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, new[] { typeof(string) });

            // These SHOULD NOT appear (suppressed by patch)
            logMethod?.Invoke(lidgrenPeer, new object[] { "Socket threw exception; would block - send buffer full? Increase in NetPeerConfiguration" });
            logMethod?.Invoke(lidgrenPeer, new object[] { "Ignoring multiple Connect() most likely due to a delayed Approval" });
            logMethod?.Invoke(lidgrenPeer, new object[] { "Received unhandled library message Ping from 127.0.0.1:1234" });

            // This SHOULD appear
            logMethod?.Invoke(lidgrenPeer, new object[] { "TEST: This log should NOT be suppressed" });
        }
    }
}
