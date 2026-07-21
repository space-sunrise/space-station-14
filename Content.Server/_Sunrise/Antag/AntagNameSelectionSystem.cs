using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.EUI;
using Content.Server._Sunrise.Antag.UI;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server._Sunrise.Antag;

public sealed class AntagNameSelectionSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private const string MindRoleNukeops = "MindRoleNukeops";
    private const string MindRoleNukeopsCommander = "MindRoleNukeopsCommander";
    private const string MindRoleNukeopsMedic = "MindRoleNukeopsMedic";
    private const string MindRoleLoneops = "MindRoleLoneops";
    private const string MindRoleDragon = "MindRoleDragon";
    private const string MindRoleNinja = "MindRoleNinja";

    private const string NukeopsCommanderFormat = "antag-name-format-nukeops-commander";
    private const string NukeopsMedicFormat = "antag-name-format-nukeops-medic";
    private const string NukeopsOperativeFormat = "antag-name-format-nukeops-operative";
    private const string LoneopsFormat = "antag-name-format-nukeops-loneops";
    private const string DragonFormat = "antag-name-format-dragon";
    private const string NinjaFormat = "antag-name-format-ninja";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagSelectionComponent, AfterAntagEntitySelectedEvent>(OnAfterAntagEntitySelected);
    }

    private void OnAfterAntagEntitySelected(Entity<AntagSelectionComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (args.Session == null ||
            !_player.TryGetSessionByEntity(args.EntityUid, out var session) ||
            session != args.Session)
            return;

        if (!TryGetNameSettings(args.Def, out var roleTitle, out var nameFormat))
            return;

        var eui = new AntagNameEui(
            ent.Owner,
            args.EntityUid,
            nameFormat,
            Name(args.EntityUid),
            roleTitle,
            _cfg.GetCVar(CCVars.MaxNameLength));

        _eui.OpenEui(eui, args.Session);
    }

    private static bool TryGetNameSettings(
        AntagSelectionDefinition def,
        out string roleTitle,
        out string nameFormat)
    {
        roleTitle = string.Empty;
        nameFormat = string.Empty;

        if (HasMindRole(def, MindRoleNukeopsCommander))
        {
            roleTitle = "antag-name-eui-title-nukeops-commander";
            nameFormat = NukeopsCommanderFormat;
            return true;
        }

        if (HasMindRole(def, MindRoleNukeopsMedic))
        {
            roleTitle = "antag-name-eui-title-nukeops-medic";
            nameFormat = NukeopsMedicFormat;
            return true;
        }

        if (HasMindRole(def, MindRoleNukeops))
        {
            roleTitle = "antag-name-eui-title-nukeops-operative";
            nameFormat = NukeopsOperativeFormat;
            return true;
        }

        if (HasMindRole(def, MindRoleLoneops))
        {
            roleTitle = "antag-name-eui-title-nukeops-loneops";
            nameFormat = LoneopsFormat;
            return true;
        }

        if (HasMindRole(def, MindRoleDragon))
        {
            roleTitle = "antag-name-eui-title-dragon";
            nameFormat = DragonFormat;
            return true;
        }

        if (HasMindRole(def, MindRoleNinja))
        {
            roleTitle = "antag-name-eui-title-ninja";
            nameFormat = NinjaFormat;
            return true;
        }

        return false;
    }

    private static bool HasMindRole(AntagSelectionDefinition def, string roleId)
    {
        if (def.MindRoles == null)
            return false;

        foreach (var role in def.MindRoles)
        {
            if (role.Id == roleId)
                return true;
        }

        return false;
    }
}
