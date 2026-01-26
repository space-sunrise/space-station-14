using System.Linq;
using Content.Server._Sunrise.Messenger;
using Content.Server.CartridgeLoader;
using Content.Server.PDA.Ringer;
using Content.Shared._Sunrise.CartridgeLoader.Cartridges;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared._Sunrise.Messenger;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.PDA.Ringer;

namespace Content.Server._Sunrise.CartridgeLoader.Cartridges;

/// <summary>
/// Часть системы картриджа мессенджера, отвечающая за обработку входящих пакетов
/// </summary>
public sealed partial class MessengerCartridgeSystem
{
    private void OnPacketReceived(EntityUid uid, MessengerCartridgeComponent component, CartridgeDeviceNetPacketEvent args)
    {
        var packet = args.PacketEvent;

        if (!packet.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
        {
            return;
        }

        var loaderUid = args.Loader;
        if (loaderUid == EntityUid.Invalid)
        {
            Sawmill.Warning($"Packet received but LoaderUid is invalid");
            return;
        }

        switch (command)
        {
            case MessengerCommands.CmdUserRegistered:
                HandleUserRegistered(uid, component, packet, loaderUid);
                break;
            case MessengerCommands.CmdUsersList:
                HandleUsersList(uid, component, packet, loaderUid);
                break;
            case MessengerCommands.CmdGroupsList:
                HandleGroupsList(uid, component, packet, loaderUid);
                break;
            case MessengerCommands.CmdMessagesList:
                HandleMessagesList(uid, component, packet, loaderUid);
                break;
            case MessengerCommands.CmdMessageReceived:
                HandleMessageReceived(uid, component, packet, loaderUid);
                break;
            case MessengerCommands.CmdGroupCreated:
                HandleGroupCreated(uid, component, packet, loaderUid);
                break;
            case MessengerCommands.CmdUserAddedToGroup:
                HandleUserAddedToGroup(uid, component, packet, loaderUid);
                break;
            default:
                Sawmill.Warning($"Unknown command received: {command}");
                break;
        }
    }

    private void HandleUserRegistered(EntityUid uid, MessengerCartridgeComponent component, DeviceNetworkPacketEvent packet, EntityUid loaderUid)
    {
        if (!packet.Data.TryGetValue("user_id", out string? userId))
        {
            Sawmill.Warning($"UserRegistered packet missing user_id");
            return;
        }

        Sawmill.Info($"User registered successfully: {userId}");
        component.UserId = userId;
        component.IsRegistered = true;
        component.LastRegistrationAttempt = null;

        if (TryComp<DeviceNetworkComponent>(loaderUid, out var deviceNetwork))
        {
            RequestUsers(uid, component, loaderUid, deviceNetwork);
            RequestGroups(uid, component, loaderUid, deviceNetwork);
        }

        UpdateUiState(uid, loaderUid, component);
    }

    private void HandleUsersList(EntityUid uid, MessengerCartridgeComponent component, DeviceNetworkPacketEvent args, EntityUid loaderUid)
    {
        if (!args.Data.TryGetValue("users", out List<Dictionary<string, object>>? usersData))
            return;

        var users = new List<MessengerUser>();
        foreach (var userData in usersData)
        {
            if (!userData.TryGetValue("user_id", out object? userIdObj) ||
                !userData.TryGetValue("user_name", out object? userNameObj))
                continue;

            var userId = userIdObj?.ToString();
            var userName = userNameObj?.ToString();

            if (userId == null || userName == null)
                continue;

            userData.TryGetValue("job_title", out object? jobTitleObj);
            userData.TryGetValue("department_id", out object? departmentIdObj);

            var jobTitle = jobTitleObj?.ToString();
            var departmentId = departmentIdObj?.ToString();

            users.Add(new MessengerUser(userId, userName, jobTitle, departmentId));
        }

        component.Users = users;
        UpdateUiState(uid, loaderUid, component);
    }

    private void HandleGroupsList(EntityUid uid, MessengerCartridgeComponent component, DeviceNetworkPacketEvent packet, EntityUid loaderUid)
    {
        if (!packet.Data.TryGetValue("groups", out List<Dictionary<string, object>>? groupsData))
            return;

        Dictionary<string, int>? serverUnreadCounts = null;
        if (packet.Data.TryGetValue("unread_counts", out object? unreadCountsObj))
        {
            if (unreadCountsObj is Dictionary<string, object> unreadCountsDict)
            {
                serverUnreadCounts = new Dictionary<string, int>();
                foreach (var (chatId, countObj) in unreadCountsDict)
                {
                    if (int.TryParse(countObj?.ToString(), out var count))
                    {
                        serverUnreadCounts[chatId] = count;
                    }
                }
            }
        }

        var groups = new List<MessengerGroup>();
        foreach (var groupData in groupsData)
        {
            if (!groupData.TryGetValue("group_id", out object? groupIdObj) ||
                !groupData.TryGetValue("group_name", out object? groupNameObj))
                continue;

            var groupId = groupIdObj?.ToString();
            var groupName = groupNameObj?.ToString();

            if (groupId == null || groupName == null)
                continue;

            List<string>? membersList = null;
            if (groupData.TryGetValue("members", out object? membersObj))
            {
                if (membersObj is List<object> membersObjList)
                {
                    membersList = membersObjList.Select(m => m?.ToString() ?? string.Empty).Where(m => !string.IsNullOrEmpty(m)).ToList();
                }
                else if (membersObj is List<string> membersStringList)
                {
                    membersList = membersStringList;
                }
                else if (membersObj is IEnumerable<object> membersEnumerable)
                {
                    membersList = membersEnumerable.Select(m => m?.ToString() ?? string.Empty).Where(m => !string.IsNullOrEmpty(m)).ToList();
                }
            }

            groupData.TryGetValue("group_type", out object? groupTypeObj);
            groupData.TryGetValue("auto_group_prototype_id", out object? autoGroupPrototypeIdObj);
            groupData.TryGetValue("owner_id", out object? ownerIdObj);

            var groupType = MessengerGroupType.UserCreated;
            if (groupTypeObj != null && int.TryParse(groupTypeObj.ToString(), out var typeInt))
            {
                groupType = (MessengerGroupType)typeInt;
            }

            var autoGroupPrototypeId = autoGroupPrototypeIdObj?.ToString();
            var ownerId = ownerIdObj?.ToString();

            groups.Add(new MessengerGroup(groupId, groupName, new HashSet<string>(membersList ?? new List<string>()), groupType, autoGroupPrototypeId, ownerId));
        }

        component.Groups = groups;

        if (serverUnreadCounts != null)
        {
            foreach (var (chatId, count) in serverUnreadCounts)
            {
                component.ServerUnreadCounts[chatId] = count;
            }
        }

        UpdateUiState(uid, loaderUid, component);
    }

    private void HandleMessagesList(EntityUid uid, MessengerCartridgeComponent component, DeviceNetworkPacketEvent packet, EntityUid loaderUid)
    {
        if (!packet.Data.TryGetValue("messages", out List<Dictionary<string, object>>? messagesData))
            return;

        var isStatusUpdate = packet.Data.TryGetValue("chat_id", out object? updateChatIdObj);
        var updateChatId = updateChatIdObj?.ToString();

        var chatId = isStatusUpdate ? updateChatId : component.LastRequestedChatId;
        if (string.IsNullOrEmpty(chatId))
        {
            if (messagesData.Count > 0)
            {
                var firstMessage = messagesData[0];
                firstMessage.TryGetValue("group_id", out object? groupIdObj);
                firstMessage.TryGetValue("recipient_id", out object? recipientIdObj);

                var groupId = groupIdObj?.ToString();
                var recipientId = recipientIdObj?.ToString();

                if (!string.IsNullOrEmpty(groupId))
                {
                    chatId = groupId;
                }
                else if (!string.IsNullOrEmpty(recipientId) && !string.IsNullOrEmpty(component.UserId))
                {
                    var ids = new[] { recipientId, component.UserId }.OrderBy(x => x).ToArray();
                    chatId = $"personal_{ids[0]}_{ids[1]}";
                }
            }
        }

        var messages = new List<MessengerMessage>();
        foreach (var messageData in messagesData)
        {
            if (!messageData.TryGetValue("sender_id", out object? senderIdObj) ||
                !messageData.TryGetValue("sender_name", out object? senderNameObj) ||
                !messageData.TryGetValue("content", out object? contentObj) ||
                !messageData.TryGetValue("timestamp", out object? timestampObj))
                continue;

            var senderId = senderIdObj?.ToString();
            var senderName = senderNameObj?.ToString();
            var content = contentObj?.ToString();

            if (senderId == null || senderName == null || content == null)
                continue;

            if (!double.TryParse(timestampObj?.ToString(), out var timestampSeconds))
                continue;

            messageData.TryGetValue("group_id", out object? groupIdObj);
            messageData.TryGetValue("recipient_id", out object? recipientIdObj);
            messageData.TryGetValue("is_read", out object? isReadObj);

            var groupId = groupIdObj?.ToString();
            var recipientId = recipientIdObj?.ToString();

            var isRead = false;
            if (isReadObj != null && bool.TryParse(isReadObj.ToString(), out var isReadValue))
            {
                isRead = isReadValue;
            }

            var timestamp = TimeSpan.FromSeconds(timestampSeconds);
            messages.Add(new MessengerMessage(senderId, senderName, content, timestamp, groupId, recipientId, isRead));
        }

        if (!string.IsNullOrEmpty(chatId) && messages.Count >= 0)
        {
            if (component.MessageHistory.TryGetValue(chatId, out var existingMessages))
            {
                var existingDict = new Dictionary<string, MessengerMessage>();
                for (int i = 0; i < existingMessages.Count; i++)
                {
                    var msg = existingMessages[i];
                    var key = $"{msg.SenderId}_{msg.Timestamp.TotalSeconds}_{msg.Content}_{i}";

                    if (existingDict.ContainsKey(key))
                    {
                        key = $"{msg.SenderId}_{msg.Timestamp.TotalSeconds}_{msg.Content.GetHashCode()}_{i}";
                    }
                    existingDict[key] = msg;
                }

                var hasStatusUpdate = false;
                foreach (var newMessage in messages)
                {
                    var matchingMessage = existingMessages.FirstOrDefault(m =>
                        m.SenderId == newMessage.SenderId &&
                        Math.Abs(m.Timestamp.TotalSeconds - newMessage.Timestamp.TotalSeconds) < 0.001 &&
                        m.Content == newMessage.Content);

                    if (matchingMessage != null)
                    {
                        if (matchingMessage.IsRead != newMessage.IsRead)
                        {
                            matchingMessage.IsRead = newMessage.IsRead;
                            hasStatusUpdate = true;
                        }
                    }
                }

                var newMessages = messages.Where(newMsg =>
                {
                    return !existingMessages.Any(existingMsg =>
                        existingMsg.SenderId == newMsg.SenderId &&
                        Math.Abs(existingMsg.Timestamp.TotalSeconds - newMsg.Timestamp.TotalSeconds) < 0.001 &&
                        existingMsg.Content == newMsg.Content);
                }).ToList();

                if (newMessages.Count > 0)
                {
                    existingMessages.AddRange(newMessages);
                    existingMessages = existingMessages.OrderBy(m => m.Timestamp)
                        .ThenBy(m => m.SenderId)
                        .ThenBy(m => m.Content)
                        .ToList();
                }
                component.MessageHistory[chatId] = existingMessages;
            }
            else
                component.MessageHistory[chatId] = messages.OrderBy(m => m.Timestamp).ToList();
        }

        UpdateUiState(uid, loaderUid, component);
    }

    private void HandleMessageReceived(EntityUid uid, MessengerCartridgeComponent component, DeviceNetworkPacketEvent packet, EntityUid loaderUid)
    {
        if (!packet.Data.TryGetValue("sender_id", out object? senderIdObj) ||
            !packet.Data.TryGetValue("sender_name", out object? senderNameObj) ||
            !packet.Data.TryGetValue("content", out object? contentObj) ||
            !packet.Data.TryGetValue("timestamp", out object? timestampObj))
            return;

        var senderId = senderIdObj?.ToString();
        var senderName = senderNameObj?.ToString();
        var content = contentObj?.ToString();

        if (senderId == null || senderName == null || content == null)
            return;

        if (!double.TryParse(timestampObj?.ToString(), out var timestampSeconds))
            return;

        packet.Data.TryGetValue("group_id", out object? groupIdObj);
        packet.Data.TryGetValue("recipient_id", out object? recipientIdObj);
        packet.Data.TryGetValue("is_read", out object? isReadObj);

        var groupId = groupIdObj?.ToString();
        var recipientId = recipientIdObj?.ToString();

        var isRead = false;
        if (isReadObj != null && bool.TryParse(isReadObj.ToString(), out var isReadValue))
        {
            isRead = isReadValue;
        }

        var timestamp = TimeSpan.FromSeconds(timestampSeconds);
        var message = new MessengerMessage(senderId, senderName, content, timestamp, groupId, recipientId, isRead);

        string chatId;
        if (!string.IsNullOrEmpty(groupId))
        {
            chatId = groupId;
        }
        else if (!string.IsNullOrEmpty(senderId) && !string.IsNullOrEmpty(component.UserId))
        {
            string otherUserId;
            if (component.UserId == senderId)
            {
                if (string.IsNullOrEmpty(recipientId))
                {
                    otherUserId = senderId;
                }
                else
                {
                    otherUserId = recipientId;
                }
            }
            else
            {
                otherUserId = senderId;
            }

            var ids = new[] { component.UserId, otherUserId }.OrderBy(x => x).ToArray();
            chatId = $"personal_{ids[0]}_{ids[1]}";
        }
        else
        {
            chatId = senderId;
        }

        if (!component.MessageHistory.TryGetValue(chatId, out var history))
        {
            history = new List<MessengerMessage>();
            component.MessageHistory[chatId] = history;
        }

        var messageExists = history.Any(m =>
            m.SenderId == message.SenderId &&
            Math.Abs(m.Timestamp.TotalSeconds - message.Timestamp.TotalSeconds) < 0.001 &&
            m.Content == message.Content);

        if (!messageExists)
        {
            history.Add(message);
            history = history.OrderBy(m => m.Timestamp)
                .ThenBy(m => m.SenderId)
                .ThenBy(m => m.Content)
                .ToList();
            component.MessageHistory[chatId] = history;
        }
        else
        {
            var existingMessage = history.FirstOrDefault(m =>
                m.SenderId == message.SenderId &&
                Math.Abs(m.Timestamp.TotalSeconds - message.Timestamp.TotalSeconds) < 0.001 &&
                m.Content == message.Content);

            if (existingMessage != null)
            {
                existingMessage.IsRead = message.IsRead;
            }
        }

        var isGroupChat = chatId == "common" || chatId.StartsWith("dept_") || chatId.StartsWith("group_");
        var isMuted = isGroupChat
            ? component.MutedGroupChats.Contains(chatId)
            : component.MutedPersonalChats.Contains(chatId);

        var isSender = component.UserId == senderId;
        var isChatOpen = component.LastRequestedChatId == chatId;

        if (!isSender && !isChatOpen && isGroupChat)
        {
            component.ServerUnreadCounts.TryGetValue(chatId, out var currentCount);
            component.ServerUnreadCounts[chatId] = currentCount + 1;
        }

        if (!isMuted && !isSender && TryComp<RingerComponent>(loaderUid, out var ringer))
        {
            _ringer.RingerPlayRingtone(loaderUid);
        }

        UpdateUiState(uid, loaderUid, component);

        if (TryComp<DeviceNetworkComponent>(loaderUid, out var deviceNetwork))
        {
            RequestUsers(uid, component, loaderUid, deviceNetwork);
            RequestGroups(uid, component, loaderUid, deviceNetwork);
        }
    }

    private void HandleGroupCreated(EntityUid uid, MessengerCartridgeComponent component, DeviceNetworkPacketEvent packet, EntityUid loaderUid)
    {
        if (TryComp<DeviceNetworkComponent>(loaderUid, out var deviceNetwork))
            RequestGroups(uid, component, loaderUid, deviceNetwork);
    }

    private void HandleUserAddedToGroup(EntityUid uid, MessengerCartridgeComponent component, DeviceNetworkPacketEvent packet, EntityUid loaderUid)
    {
        if (!packet.Data.TryGetValue("user_id", out object? addedUserIdObj) ||
            !packet.Data.TryGetValue("group_id", out object? groupIdObj))
            return;

        var addedUserId = addedUserIdObj?.ToString();
        var groupId = groupIdObj?.ToString();

        if (addedUserId == component.UserId && !string.IsNullOrEmpty(groupId))
        {
            if (TryComp<RingerComponent>(loaderUid, out var ringer))
            {
                _ringer.RingerPlayRingtone(loaderUid);
            }
        }

        if (TryComp<DeviceNetworkComponent>(loaderUid, out var deviceNetwork))
            RequestGroups(uid, component, loaderUid, deviceNetwork);
    }
}
