#if SUNRISE_PRIVATE
using Content.Sunrise.Interfaces.Client;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Utility;
#endif

namespace Content.Client._Sunrise.Entry;

public sealed class SunriseClientEntry
{
    public static void Init()
    {
    }

    public static void PostInit()
    {
#if SUNRISE_PRIVATE
        IoCManager.Resolve<ISharedSponsorsManager>().Initialize();
        IoCManager.Resolve<IClientJoinQueueManager>().Initialize();
        IoCManager.Resolve<IClientServiceAuthManager>().Initialize();
        IoCManager.Resolve<IClientServiceCheckMemberManager>().Initialize();
#endif
    }
}
