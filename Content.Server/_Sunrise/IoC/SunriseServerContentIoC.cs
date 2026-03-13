#if SUNRISE_PRIVATE
using Content.Server._SunrisePrivate.JoinQueue;
using Content.Server._SunrisePrivate.ServiceAuth;
using Content.Server._SunrisePrivate.Sponsors;
using Content.Server._SunrisePrivate.AntiNuke;
using Content.Sunrise.Interfaces.Server;
using Content.Sunrise.Interfaces.Shared;
#endif

namespace Content.Server._Sunrise.IoC;

internal static class SunriseServerContentIoC
{
    public static void Register()
    {
#if SUNRISE_PRIVATE
        IoCManager.Register<ISharedSponsorsManager, ServerSponsorsManager>();
        IoCManager.Register<IServerServiceAuthManager, ServiceAuthManager>();
        IoCManager.Register<IServerJoinQueueManager, JoinQueueManager>();
        IoCManager.Register<AntiNukeManager>();
#endif
    }
}
