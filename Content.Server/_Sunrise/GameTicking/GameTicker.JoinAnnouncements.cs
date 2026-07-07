using System.Globalization;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Roles;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала
namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    partial void DispatchLateJoinAnnouncementPortal(
        EntityUid station,
        EntityUid mob,
        JobPrototype jobPrototype,
        HumanoidCharacterProfile character,
        string jobName)
    {
        if (!jobPrototype.JoinNotifyCrew)
            return;

        _chatSystem.DispatchStationAnnouncement(station,
            Loc.GetString("latejoin-arrival-announcement-special",
                ("character", MetaData(mob).EntityName),
                ("gender", character.Gender), // Russian-LastnameGender
                ("entity", mob),
                ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobName))),
            Loc.GetString("latejoin-arrival-sender"),
            playDefault: false,
            colorOverride: Color.Gold);
    }
}
