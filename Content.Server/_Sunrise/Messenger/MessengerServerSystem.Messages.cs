using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared._Sunrise.Messenger;

namespace Content.Server._Sunrise.Messenger;

/// <summary>
/// Часть системы мессенджера, отвечающая за обработку сообщений
/// </summary>
public sealed partial class MessengerServerSystem
{
    private void HandleSendMessage(EntityUid uid, MessengerServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(MessengerCommands.CmdSendMessage, out NetworkPayload? messageData))
            return;

        if (!messageData.TryGetValue("content", out string? content) || string.IsNullOrWhiteSpace(content))
            return;

        if (!component.Users.TryGetValue(args.SenderAddress, out var sender))
            return;

        var timestamp = GetStationTime();

        if (messageData.TryGetValue("group_id", out string? groupId) && !string.IsNullOrWhiteSpace(groupId))
        {
            SendGroupMessage(uid, component, sender, groupId, content, timestamp);
        }
        else if (messageData.TryGetValue("recipient_id", out string? recipientId) && !string.IsNullOrWhiteSpace(recipientId))
        {
            SendPersonalMessage(uid, component, sender, recipientId, content, timestamp);
        }
    }

    private void SendPersonalMessage(EntityUid uid, MessengerServerComponent component, MessengerUser sender, string recipientId, string content, TimeSpan timestamp)
    {
        if (!component.Users.ContainsKey(recipientId))
            return;

        var message = new MessengerMessage(sender.UserId, sender.Name, content, timestamp, null, recipientId, isRead: false);
        var chatId = GetPersonalChatId(sender.UserId, recipientId);

        if (!component.MessageHistory.TryGetValue(chatId, out var history))
        {
            history = new List<MessengerMessage>();
            component.MessageHistory[chatId] = history;
        }

        history.Add(message);
        TrimMessageHistory(history, component.MaxMessageHistory);

        var isChatOpen = component.OpenChats.TryGetValue(recipientId, out var openChatId) && openChatId == chatId;

        if (isChatOpen)
        {
            message.IsRead = true;
        }
        else
        {
            if (!component.UnreadCounts.TryGetValue(recipientId, out var recipientUnreads))
            {
                recipientUnreads = new Dictionary<string, int>();
                component.UnreadCounts[recipientId] = recipientUnreads;
            }
            recipientUnreads.TryGetValue(chatId, out var currentCount);
            recipientUnreads[chatId] = currentCount + 1;
        }

        if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
            return;

        uint? pdaFrequency = null;
        if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var pdaFreq))
        {
            pdaFrequency = pdaFreq.Frequency;
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = MessengerCommands.CmdMessageReceived,
            ["sender_id"] = message.SenderId,
            ["sender_name"] = message.SenderName,
            ["content"] = message.Content,
            ["timestamp"] = message.Timestamp.TotalSeconds,
            ["group_id"] = message.GroupId ?? string.Empty,
            ["recipient_id"] = message.RecipientId ?? string.Empty,
            ["is_read"] = message.IsRead
        };

        if (pdaFrequency.HasValue)
        {
            _deviceNetwork.QueuePacket(uid, recipientId, payload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
            _deviceNetwork.QueuePacket(uid, sender.UserId, payload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
        }
        else
        {
            _deviceNetwork.QueuePacket(uid, recipientId, payload);
            _deviceNetwork.QueuePacket(uid, sender.UserId, payload);
        }

        if (isChatOpen && pdaFrequency.HasValue)
        {
            var updatePayload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = MessengerCommands.CmdMessagesList,
                ["messages"] = new List<Dictionary<string, object>>
                {
                    new ()
                    {
                        ["sender_id"] = message.SenderId,
                        ["sender_name"] = message.SenderName,
                        ["content"] = message.Content,
                        ["timestamp"] = message.Timestamp.TotalSeconds,
                        ["group_id"] = message.GroupId ?? string.Empty,
                        ["recipient_id"] = message.RecipientId ?? string.Empty,
                        ["is_read"] = message.IsRead
                    }
                },
                ["chat_id"] = chatId
            };
            _deviceNetwork.QueuePacket(uid, sender.UserId, updatePayload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
        }
    }

    private void SendGroupMessage(EntityUid uid, MessengerServerComponent component, MessengerUser sender, string groupId, string content, TimeSpan timestamp)
    {
        if (!component.Groups.TryGetValue(groupId, out var group))
            return;

        if (!group.Members.Contains(sender.UserId))
            return;

        var message = new MessengerMessage(sender.UserId, sender.Name, content, timestamp, groupId);

        if (!component.MessageHistory.TryGetValue(groupId, out var history))
        {
            history = new List<MessengerMessage>();
            component.MessageHistory[groupId] = history;
        }

        history.Add(message);
        TrimMessageHistory(history, component.MaxMessageHistory);

        if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
            return;

        uint? pdaFrequency = null;
        if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var pdaFreq))
        {
            pdaFrequency = pdaFreq.Frequency;
        }

        foreach (var memberId in group.Members)
        {
            if (memberId == sender.UserId)
                continue;

            var isMemberChatOpen = component.OpenChats.TryGetValue(memberId, out var memberOpenChatId) && memberOpenChatId == groupId;

            if (!isMemberChatOpen)
            {
                if (!component.UnreadCounts.TryGetValue(memberId, out var memberUnreads))
                {
                    memberUnreads = new Dictionary<string, int>();
                    component.UnreadCounts[memberId] = memberUnreads;
                }
                memberUnreads.TryGetValue(groupId, out var currentCount);
                memberUnreads[groupId] = currentCount + 1;
            }
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = MessengerCommands.CmdMessageReceived,
            ["sender_id"] = message.SenderId,
            ["sender_name"] = message.SenderName,
            ["content"] = message.Content,
            ["timestamp"] = message.Timestamp.TotalSeconds,
            ["group_id"] = message.GroupId ?? string.Empty,
            ["recipient_id"] = message.RecipientId ?? string.Empty,
            ["is_read"] = message.IsRead
        };

        foreach (var memberId in group.Members)
        {
            if (pdaFrequency.HasValue)
            {
                _deviceNetwork.QueuePacket(uid, memberId, payload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
            }
            else
            {
                _deviceNetwork.QueuePacket(uid, memberId, payload);
            }
        }
    }
}
