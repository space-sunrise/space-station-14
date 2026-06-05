using Content.Shared._Sunrise.Localization;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

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
        var loc = IoCManager.Resolve<ILocalizationManager>();
        foreach (var culture in loc.GetFoundCultures())
        {
            if (!loc.HasCulture(culture))
                continue;

            loc.AddFunction(culture, "KEYBIND", KeybindLocalization.FormatKeybind);
        }

        var input = IoCManager.Resolve<IInputManager>();
        KeybindLocalization.ResolveKeybind = functionName =>
        {
            var function = new BoundKeyFunction(functionName);
            return input.TryGetKeyBinding(function, out var binding)
                ? binding.GetKeyString()
                : null;
        };
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
