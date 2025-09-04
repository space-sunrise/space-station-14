

using Content.Server._Sunrise.TTS;
using Robust.Shared.IoC;

namespace Content.Server._Sunrise.IoC;

internal static class SunriseServerContentIoC
{
    public static void Register()
    {
        IoCManager.Register<TTSManager>();
    }
}
