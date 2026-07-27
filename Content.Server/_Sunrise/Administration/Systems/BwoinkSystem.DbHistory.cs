using System.Linq;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems;

public partial class BwoinkSystem
{
    private async void OnRequestDbMessages(BwoinkRequestDbMessages message, EntitySessionEventArgs args)
    {
        var isAdmin = _adminManager.IsAdmin(args.SenderSession);
        if (!isAdmin && args.SenderSession.UserId != message.UserId)
            return;

        var history = new List<BwoinkTextMessage>();
        var messages = (await _dbManager.GetAHelpMessagesByReceiverAsync(message.UserId)).OrderBy(x => x.SentAt);

        foreach (var entry in messages)
        {
            var senderId = (NetUserId) entry.SenderUserId;

            if (!isAdmin && entry.AdminOnly && senderId != args.SenderSession.UserId)
                continue;

            var senderRecord = await _dbManager.GetPlayerRecordByUserId(senderId);
            var name = senderRecord?.LastSeenUserName ?? senderId.ToString();

            AdminData? senderAdminData = null;
            ICommonSession? senderSession = null;

            if (_playerManager.TryGetSessionById(senderId, out var session))
            {
                senderSession = session;
                senderAdminData = _adminManager.GetAdminData(session);
            }
            else
            {
                var loadedAdminData = await _adminManager.LoadAdminData(senderId);
                if (loadedAdminData is not null)
                    senderAdminData = loadedAdminData.Value.dat;
            }

            var adminPrefix = "";
            if (_config.GetCVar(CCVars.AhelpAdminPrefix) && senderAdminData?.Title != null)
                adminPrefix = $"[bold]\\[{FormattedMessage.EscapeText(senderAdminData.Title)}\\][/bold] ";

            string formattedName;
            if (senderSession != null)
            {
                var nameToFormat = isAdmin || _overrideClientName == string.Empty ? name : _overrideClientName;
                formattedName = FormatName(senderAdminData, senderSession, adminPrefix, nameToFormat);
            }
            else
            {
                if (senderAdminData is not null && senderAdminData.Flags == AdminFlags.Adminhelp)
                {
                    formattedName = $"[color=purple]{adminPrefix}{name}[/color]";
                }
                else if (senderAdminData is not null && senderAdminData.HasFlag(AdminFlags.Adminhelp))
                {
                    formattedName = $"[color=red]{adminPrefix}{name}[/color]";
                }
                else
                {
                    formattedName = $"{adminPrefix}{name}";
                }
            }

            string text;
            if (isAdmin)
            {
                text = await GenerateNameLinks(entry.Message);
            }
            else
            {
                text = FormattedMessage.EscapeText(entry.Message);
            }

            var statusPrefix = entry.AdminOnly ? Loc.GetString("bwoink-message-admin-only") :
                !entry.PlaySound ? Loc.GetString("bwoink-message-silent") : string.Empty;

            var finalMessageText = $"{statusPrefix} {formattedName}: {text}".TrimStart();

            history.Add(new BwoinkTextMessage(
                (NetUserId) entry.ReceiverUserId,
                senderId,
                finalMessageText,
                entry.SentAt.DateTime.ToLocalTime(),
                entry.PlaySound,
                entry.AdminOnly,
                dbLoad: true));
        }

        RaiseNetworkEvent(new BwoinkTextHistoryMessage(message.UserId, history), args.SenderSession.Channel);
    }

    partial void OnBwoinkMessagePersisted(BwoinkTextMessage message, ICommonSession sender)
    {
        _ = _dbManager.AddAHelpMessage(sender.UserId.UserId, message.UserId.UserId, message.Text,
            message.SentAt, message.PlaySound, message.AdminOnly);
    }
}
