#if SUNRISE_PRIVATE
using Content.Client._SunrisePrivate.JoinQueue;
using Content.Client._SunrisePrivate.ServiceAuth;
using Content.Client._SunrisePrivate.Sponsors;
using Content.Sunrise.Interfaces.Client;
using Content.Sunrise.Interfaces.Shared;
#endif

namespace Content.Client._Sunrise.IoC;

internal static class SunriseClientContentIoC
{
    public static void Register()
    {
#if SUNRISE_PRIVATE
        var collection = IoCManager.Instance!;
        collection.Register<ISharedSponsorsManager, ClientSponsorsManager>();
        collection.Register<IClientJoinQueueManager, JoinQueueManager>();
        collection.Register<IClientServiceAuthManager, ClientServiceAuthManager>();
        collection.Register<IClientServiceCheckMemberManager, ClientServiceCheckMemberManager>();
#endif
    }
}
