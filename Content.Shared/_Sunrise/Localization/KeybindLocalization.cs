using System;
using Robust.Shared.Localization;

namespace Content.Shared._Sunrise.Localization;

public static class KeybindLocalization
{
    public static Func<string, string?>? ResolveKeybind;

    public static ILocValue FormatKeybind(LocArgs args)
    {
        if (args.Args.Count < 1)
            return new LocValueString("");

        var functionName = args.Args[0].Value?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(functionName))
            return new LocValueString("");

        var keybind = ResolveKeybind?.Invoke(functionName);
        if (string.IsNullOrEmpty(keybind))
            keybind = Loc.GetString("ui-options-unbound");

        return new LocValueString(keybind);
    }
}
