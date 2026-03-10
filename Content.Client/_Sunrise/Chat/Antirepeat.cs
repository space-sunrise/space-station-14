using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Chat;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Chat;

public sealed class Antirepeat
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    private int _repeatHistory;

    public void Initialize()
    {
        SubscribeCVar();
    }

    public void Shutdown()
    {
        UnsubscribeCVar();
    }

    private void SubscribeCVar()
    {
        _config.OnValueChanged(SunriseCCVars.AntiSpamChatRepeatHistory, v => _repeatHistory = v, true);
    }

    private void UnsubscribeCVar()
    {
        _config.UnsubValueChanged(SunriseCCVars.AntiSpamChatRepeatHistory, v => _repeatHistory = v);
    }

    public bool TryRepetition(
        ChatBox chat,
        OutputPanel contents,
        FormattedMessage message,
        NetEntity sender,
        string unwrapped,
        ChatChannel channel,
        bool repeatCheckSender
    )
    {
        if (_repeatHistory <= 0)
        {
            chat.RepeatQueue.Clear();
            return false;
        }

        var repeated = false;

        foreach (var old in chat.RepeatQueue)
        {
            if (!old.Message.Equals(unwrapped) || old.Channel != channel)
                continue;

            if (repeatCheckSender && !old.SenderEntity.Equals(sender))
                continue;

            old.Count++;
            var updated = new FormattedMessage(old.FormattedMessage);
            updated.AddMarkupPermissive($" [color=red]x{old.Count}[/color]");
            contents.SetMessage(old.Index, updated);
            repeated = true;
            break;
        }

        if (!repeated)
        {
            chat.RepeatQueue.Enqueue(new RepeatedMessage(contents.EntryCount, message, sender, unwrapped, channel));

            while (chat.RepeatQueue.Count > _repeatHistory)
                chat.RepeatQueue.Dequeue();
        }

        return repeated;
    }
}
