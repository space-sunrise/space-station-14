using System.Linq;
using Content.Shared.Administration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems;

public partial class BwoinkSystem
{
    private async void OnRequestDbMessages(BwoinkRequestDbMessages message, EntitySessionEventArgs args)
    {
        if (!_adminManager.IsAdmin(args.SenderSession))
            return;

        var history = new List<BwoinkTextMessage>();
        var messages = (await _dbManager.GetAHelpMessagesByReceiverAsync(message.UserId)).OrderBy(x => x.SentAt);

        foreach (var entry in messages)
        {
            var sender = await _dbManager.GetPlayerRecordByUserId((NetUserId) entry.SenderUserId);
            var name = sender?.LastSeenUserName ?? entry.SenderUserId.ToString();
            var text = FormattedMessage.EscapeText(entry.Message);
            var prefix = entry.AdminOnly ? Loc.GetString("bwoink-message-admin-only") :
                !entry.PlaySound ? Loc.GetString("bwoink-message-silent") : string.Empty;

            history.Add(new BwoinkTextMessage(
                (NetUserId) entry.ReceiverUserId,
                (NetUserId) entry.SenderUserId,
                $"{prefix} {name}: {text}",
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
