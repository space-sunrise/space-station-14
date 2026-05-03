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

    }
}
