// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using System.Diagnostics.CodeAnalysis;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using Robust.Shared.ViewVariables;

namespace Content.Anticheat.Client.Systems;

public sealed class AntiCheatChecks : EntitySystem
{
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IEntitySystemManager _esm = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    string[] _allowed =
    [
        "Content.Client",
        "Content.Shared",
        "Content.Server",
        "Content.Shared.Database",
        "Robust.Client",
        "Robust.Shared",
        "Robust.Server",
        "Content.Anticheat",
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(LobbyJoinChecks);
    }

    private void LobbyJoinChecks(LocalPlayerAttachedEvent ev)
    {
        Check();
    }

    public void Check()
    {
        Log.Info("Running tests");

        if (FoundPatchMetadataTypes())
            Log.Warning("Detected a patch metadata type directly!");

        if (FoundExtraTypesIReflection(out var offend))
            Log.Warning($"Detected a patch metadata type through IReflection! Offender: {offend}");

        if (FoundMoonywareModuleReflection())
            Log.Warning("Detected Moonyware in IReflection!");

        if (TypesNotFromContentIoC(out offend))
            Log.Warning($"Detected a type not from a Content module! Offender: {offend}");

        if (CheckExtraModule(out offend))
            Log.Warning($"Detected an extra module! Offender: {offend}");

        if (CheckCommonCheatCvars(out offend))
            Log.Warning($"Detected a suspicious cvar! Offender: {offend}");

        if (FoundTypesEntitySystemManager(out offend))
            Log.Warning($"Detected extra types from EntitySystemManager! Offender: {offend}");

        if (CheckComponents(out offend))
            Log.Warning($"Detected an extra component on player entity! Offender: {offend}");

        if (CheckExtraWindows(out offend))
            Log.Warning($"Detected extra UI window! Offender: {offend}");
    }

    private bool FoundPatchMetadataTypes()
    {
        var found = Type.GetType("MarseyPatch") ?? Type.GetType("SubverterPatch");

        return found is not null;
    }

    private bool FoundExtraTypesIReflection([NotNullWhen(true)] out string? offender)
    {
        offender = null;
        string[] typenames = ["SubverterPatch", "MarseyPatch", "MarseyEntry", "Sedition"];

        var types = _reflection.FindAllTypes();

        foreach (var type in types)
        {
            foreach (var name in typenames)
            {
                if (!type.Name.Contains(name))
                    continue;

                offender = type.Name;
                return true;
            }
        }

        return false;
    }

    private bool FoundMoonywareModuleReflection()
    {
        var modules = _reflection.Assemblies;

        foreach (var asm in modules)
        {
            if (asm.FullName!.Contains("Moonyware"))
                return true;
        }

        return false;
    }

    private bool FoundTypesEntitySystemManager([NotNullWhen(true)] out string? offend)
    {
        offend = null;

        var types = _esm.GetEntitySystemTypes();

        foreach (var type in types)
        {
            if (!NotFromGameModule(type))
                continue;

            offend = type.FullName!;
            return true;
        }

        return false;
    }

    private bool TypesNotFromContentIoC([NotNullWhen(true)] out string? offend)
    {
        offend = null;

        var types = IoCManager.Instance!.GetRegisteredTypes();

        foreach (var type in types)
        {
            if (!NotFromGameModule(type))
                continue;

            offend = type.FullName!;
            return true;
        }

        return false;
    }

    private bool CheckExtraModule([NotNullWhen(true)] out string? offend)
    {
        offend = null;

        var modules = _reflection.Assemblies;

        foreach (var module in modules)
        {
            var  allowed = false;

            foreach (var allow in _allowed)
            {
                if (module.FullName!.Contains(allow))
                {
                    allowed = true;
                    break;
                }
            }

            if (allowed)
                continue;

            offend = module.FullName!;
            return true;
        }

        return false;
    }

    private bool CheckCommonCheatCvars([NotNullWhen(true)] out string? offend)
    {
        string[] keywords =
        [
            "aimbot",
            "esp",
            "noslip",
            "exploit",
        ];

        offend = null;

        var cvars = _configuration.GetRegisteredCVars();

        foreach (var cvar in cvars)
        {
            if (!keywords.Any(kw => cvar.Contains(kw, StringComparison.CurrentCultureIgnoreCase)))
                continue;

            offend = cvar;
            return true;
        }

        return false;
    }

    private bool CheckComponents([NotNullWhen(true)] out string? offend)
    {
        offend = null;

        if (_player.LocalEntity is null)
            return false;

        var comps = AllComps(_player.LocalEntity!.Value);

        foreach (var comp in comps)
        {
            var type = comp.GetType();

            if (!NotFromGameModule(type))
                continue;

            offend = type.FullName!;
            return true;
        }

        return false;
    }

    private bool CheckExtraWindows([NotNullWhen(true)] out string? offend)
    {
        offend = null;

        var children = _ui.WindowRoot.Children;

        foreach (var child in children)
        {
            var type = child.GetType();

            if (!NotFromGameModule(type))
                continue;

            offend = type.FullName!;
            return true;
        }

        return false;
    }

    private bool NotFromGameModule(Type type)
    {
        var name = type.FullName;

        foreach (var allow in _allowed)
        {
            if (name!.Contains(allow))
                return false;
        }

        return true;
    }
}

sealed class RecheckCommand : LocalizedEntityCommands
{
    public override string Command => "Anticheat.Recheck";

    [Dependency] private readonly AntiCheatChecks _ac = default!;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _ac.Check();
    }
}
