using System.Linq;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork.Events;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.GameTicking;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Messenger;
using Content.Shared.PDA;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Server.Messenger;

/// <summary>
/// Система сервера мессенджера, обрабатывающая сообщения между КПК
/// </summary>
public sealed class MessengerServerSystem : EntitySystem
{
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServer = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    private ISawmill _sawmill = default!;

    private const string CmdRegisterUser = "messenger_register_user";
    private const string CmdSendMessage = "messenger_send_message";
    private const string CmdCreateGroup = "messenger_create_group";
    private const string CmdAddToGroup = "messenger_add_to_group";
    private const string CmdRemoveFromGroup = "messenger_remove_from_group";
    private const string CmdGetUsers = "messenger_get_users";
    private const string CmdGetGroups = "messenger_get_groups";
    private const string CmdGetMessages = "messenger_get_messages";

    private const string CmdUserRegistered = "messenger_user_registered";
    private const string CmdUsersList = "messenger_users_list";
    private const string CmdGroupsList = "messenger_groups_list";
    private const string CmdMessagesList = "messenger_messages_list";
    private const string CmdMessageReceived = "messenger_message_received";
    private const string CmdGroupCreated = "messenger_group_created";
    private const string CmdUserAddedToGroup = "messenger_user_added_to_group";

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("messenger.server");

        SubscribeLocalEvent<MessengerServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<MessengerServerComponent, DeviceNetServerConnectedEvent>(OnServerConnected);
        SubscribeLocalEvent<MessengerServerComponent, DeviceNetServerDisconnectedEvent>(OnServerDisconnected);
        SubscribeLocalEvent<MessengerServerComponent, RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnRoundRestart(EntityUid uid, MessengerServerComponent component, RoundRestartCleanupEvent args)
    {
        component.Users.Clear();
        component.Groups.Clear();
        component.MessageHistory.Clear();
        component.GroupIdCounter = 0;
    }

    private void OnServerConnected(EntityUid uid, MessengerServerComponent component, ref DeviceNetServerConnectedEvent args)
    {
        _sawmill.Info($"Messenger server connected: {ToPrettyString(uid)}");

        if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
        {
            _sawmill.Error($"Server DeviceNetwork component not found: {ToPrettyString(uid)}");
            return;
        }

        _sawmill.Info($"Server DeviceNetwork before connect: Address={serverDevice.Address}, TransmitFrequency={serverDevice.TransmitFrequency}, ReceiveFrequency={serverDevice.ReceiveFrequency}, TransmitFrequencyId={serverDevice.TransmitFrequencyId}, ReceiveFrequencyId={serverDevice.ReceiveFrequencyId}");

        if (serverDevice.ReceiveFrequency == null && serverDevice.ReceiveFrequencyId != null)
        {
            if (_prototypeManager.TryIndex<DeviceFrequencyPrototype>(serverDevice.ReceiveFrequencyId, out var receiveFreq))
            {
                _deviceNetwork.SetReceiveFrequency(uid, receiveFreq.Frequency, serverDevice);
            }
        }

        if (serverDevice.TransmitFrequency == null && serverDevice.TransmitFrequencyId != null)
        {
            if (_prototypeManager.TryIndex<DeviceFrequencyPrototype>(serverDevice.TransmitFrequencyId, out var transmitFreq))
            {
                _deviceNetwork.SetTransmitFrequency(uid, transmitFreq.Frequency, serverDevice);
            }
        }

        if (!_deviceNetwork.IsDeviceConnected(uid, serverDevice))
        {
            if (!_deviceNetwork.ConnectDevice(uid, serverDevice))
            {
                return;
            }
        }

        CreateAutoGroups(component);

        foreach (var user in component.Users.Values)
        {
            AddUserToAutoGroups(uid, component, user.UserId, user.Name, user.DepartmentId);
        }
    }

    private void OnServerDisconnected(EntityUid uid, MessengerServerComponent component, ref DeviceNetServerDisconnectedEvent args)
    {
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        EntityUid? pdaUid = null;

        if (_inventory.TryGetSlotEntity(args.Mob, "idcard", out var idCardEntity) &&
            TryComp<PdaComponent>(idCardEntity, out _))
        {
            pdaUid = idCardEntity;
        }
        else if (_inventory.TryGetSlotEntity(args.Mob, "belt", out var beltEntity) &&
                 TryComp<PdaComponent>(beltEntity, out _))
        {
            pdaUid = beltEntity;
        }
        else
        {
            var handsQuery = EntityQueryEnumerator<PdaComponent>();
            while (handsQuery.MoveNext(out var uid, out var pda))
            {
                if (pda.PdaOwner == args.Mob)
                {
                    pdaUid = uid;
                    break;
                }
            }
        }

        if (pdaUid == null)
        {
            _sawmill.Warning($"No PDA found for player: {ToPrettyString(args.Mob)}");
            return;
        }

        var station = _stationSystem.GetOwningStation(args.Mob);
        if (station == null)
        {
            _sawmill.Warning($"No station found for player: {ToPrettyString(args.Mob)}");
            return;
        }

        _sawmill.Debug($"Player station: {ToPrettyString(station.Value)}");

        var serverQuery = EntityQueryEnumerator<MessengerServerComponent, SingletonDeviceNetServerComponent, DeviceNetworkComponent>();
        EntityUid? serverUid = null;
        MessengerServerComponent? serverComponent = null;
        DeviceNetworkComponent? serverDevice = null;

        int serverCount = 0;
        while (serverQuery.MoveNext(out var uid, out var comp, out var singleton, out var device))
        {
            serverCount++;
            var serverStation = _stationSystem.GetOwningStation(uid);

            if (serverStation != station)
                continue;

            if (!_singletonServer.IsActiveServer(uid, singleton))
            {
                continue;
            }

            serverUid = uid;
            serverComponent = comp;
            serverDevice = device;
            break;
        }

        if (serverCount == 0)
        {
            _sawmill.Warning($"No messenger servers found on station: {ToPrettyString(station.Value)}");
        }

        if (serverUid == null || serverComponent == null || serverDevice == null)
        {
            _sawmill.Warning($"No active messenger server found for player: {ToPrettyString(args.Mob)}");
            return;
        }

        _sawmill.Debug($"Server DeviceNetwork before registration: Address={serverDevice.Address}, TransmitFrequency={serverDevice.TransmitFrequency}, ReceiveFrequency={serverDevice.ReceiveFrequency}");

        RegisterUserFromPda(serverUid.Value, serverComponent, pdaUid.Value);
    }

    /// <summary>
    /// Регистрирует пользователя на сервере мессенджера по его PDA
    /// </summary>
    private void RegisterUserFromPda(EntityUid uid, MessengerServerComponent component, EntityUid pdaUid)
    {

        if (!TryComp<PdaComponent>(pdaUid, out var pda))
        {
            _sawmill.Warning($"PDA component not found: {ToPrettyString(pdaUid)}");
            return;
        }

        if (!TryComp<DeviceNetworkComponent>(pdaUid, out var pdaDevice))
        {
            _sawmill.Warning($"DeviceNetwork component not found on PDA: {ToPrettyString(pdaUid)}");
            return;
        }

        if (string.IsNullOrEmpty(pdaDevice.Address))
        {
            return;
        }

        var userId = pdaDevice.Address;
        var userName = pda.OwnerName ?? Loc.GetString("messenger-user-unknown");

        string? jobTitle = null;
        string? departmentId = null;

        if (pda.ContainedId != null && TryComp<IdCardComponent>(pda.ContainedId.Value, out var idCard))
        {
            jobTitle = idCard.LocalizedJobTitle;
            if (idCard.JobDepartments.Count > 0)
            {
                departmentId = idCard.JobDepartments[0];
            }
        }

        var user = new MessengerUser(userId, userName, jobTitle, departmentId);
        component.Users[userId] = user;

        AddUserToAutoGroups(uid, component, userId, userName, departmentId);
    }

    /// <summary>
    /// Создает автоматические группы на основе прототипов
    /// </summary>
    private void CreateAutoGroups(MessengerServerComponent component)
    {
        int created = 0;
        foreach (var autoGroupProto in _prototypeManager.EnumeratePrototypes<MessengerAutoGroupPrototype>())
        {
            if (component.Groups.ContainsKey(autoGroupProto.GroupId))
                continue;

            var group = new MessengerGroup(
                autoGroupProto.GroupId,
                _loc.GetString(autoGroupProto.Name),
                new HashSet<string>(),
                MessengerGroupType.Automatic,
                autoGroupProto.ID
            );
            component.Groups[autoGroupProto.GroupId] = group;
            created++;
        }

        _sawmill.Info($"Created {created} automatic messenger groups");
    }

    /// <summary>
    /// Добавляет пользователя в автоматические группы на основе прототипов
    /// </summary>
    private void AddUserToAutoGroups(EntityUid uid, MessengerServerComponent component, string userId, string userName, string? departmentId)
    {
        foreach (var autoGroupProto in _prototypeManager.EnumeratePrototypes<MessengerAutoGroupPrototype>())
        {
            bool shouldAdd = false;

            if (autoGroupProto.AddAllUsers)
            {
                shouldAdd = true;
            }
            else if (departmentId != null && autoGroupProto.Departments.Count > 0)
            {
                shouldAdd = autoGroupProto.Departments.Contains(departmentId);
            }

            if (!shouldAdd)
                continue;

            if (!component.Groups.TryGetValue(autoGroupProto.GroupId, out var group))
            {
                group = new MessengerGroup(
                    autoGroupProto.GroupId,
                    _loc.GetString(autoGroupProto.Name),
                    new HashSet<string>(),
                    MessengerGroupType.Automatic,
                    autoGroupProto.ID
                );
                component.Groups[autoGroupProto.GroupId] = group;
            }

            if (!group.Members.Contains(userId))
            {
                group.Members.Add(userId);

                var timestamp = GetStationTime();
                var messageText = Loc.GetString("messenger-system-user-added", ("userName", userName));
                var systemMessage = new MessengerMessage("system", Loc.GetString("messenger-system-name"), messageText, timestamp, autoGroupProto.GroupId);

                if (!component.MessageHistory.TryGetValue(autoGroupProto.GroupId, out var history))
                {
                    history = new List<MessengerMessage>();
                    component.MessageHistory[autoGroupProto.GroupId] = history;
                }
                history.Add(systemMessage);
                TrimMessageHistory(history, component.MaxMessageHistory);

                if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
                    continue;

                uint? pdaFrequency = null;
                if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var pdaFreq))
                {
                    pdaFrequency = pdaFreq.Frequency;
                }

                var messagePayload = new NetworkPayload
                {
                    [DeviceNetworkConstants.Command] = CmdMessageReceived,
                    ["sender_id"] = "system",
                    ["sender_name"] = Loc.GetString("messenger-system-name"),
                    ["content"] = messageText,
                    ["timestamp"] = timestamp.TotalSeconds,
                    ["group_id"] = autoGroupProto.GroupId,
                    ["recipient_id"] = string.Empty,
                    ["is_read"] = false
                };

                foreach (var memberId in group.Members)
                {
                    if (memberId == userId)
                        continue;

                    var isMemberChatOpen = component.OpenChats.TryGetValue(memberId, out var memberOpenChatId) && memberOpenChatId == autoGroupProto.GroupId;

                    if (!isMemberChatOpen)
                    {
                        if (!component.UnreadCounts.TryGetValue(memberId, out var memberUnreads))
                        {
                            memberUnreads = new Dictionary<string, int>();
                            component.UnreadCounts[memberId] = memberUnreads;
                        }
                        memberUnreads.TryGetValue(autoGroupProto.GroupId, out var currentCount);
                        memberUnreads[autoGroupProto.GroupId] = currentCount + 1;
                    }

                    if (pdaFrequency.HasValue)
                    {
                        _deviceNetwork.QueuePacket(uid, memberId, messagePayload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
                    }
                    else
                    {
                        _deviceNetwork.QueuePacket(uid, memberId, messagePayload);
                    }
                }

                var payload = new NetworkPayload
                {
                    [DeviceNetworkConstants.Command] = CmdUserAddedToGroup,
                    ["group_id"] = autoGroupProto.GroupId,
                    ["user_id"] = userId
                };

                foreach (var memberId in group.Members)
                {
                    if (memberId == userId)
                        continue;

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
    }

    private void OnPacketReceived(EntityUid uid, MessengerServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!_singletonServer.IsActiveServer(uid))
        {
            return;
        }

        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
        {
            return;
        }

        switch (command)
        {
            case CmdRegisterUser:
                HandleRegisterUser(uid, component, args);
                break;
            case CmdSendMessage:
                HandleSendMessage(uid, component, args);
                break;
            case CmdCreateGroup:
                HandleCreateGroup(uid, component, args);
                break;
            case CmdAddToGroup:
                HandleAddToGroup(uid, component, args);
                break;
            case CmdRemoveFromGroup:
                HandleRemoveFromGroup(uid, component, args);
                break;
            case CmdGetUsers:
                HandleGetUsers(uid, component, args);
                break;
            case CmdGetGroups:
                HandleGetGroups(uid, component, args);
                break;
            case CmdGetMessages:
                HandleGetMessages(uid, component, args);
                break;
            default:
                _sawmill.Warning($"Unknown command received: {command} from {args.SenderAddress}");
                break;
        }
    }

    private void HandleRegisterUser(EntityUid uid, MessengerServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(CmdRegisterUser, out NetworkPayload? userData))
        {
            return;
        }

        if (!userData.TryGetValue("pda_uid", out NetEntity netPdaUid))
        {
            return;
        }

        var pdaUid = EntityManager.GetEntity(netPdaUid);

        if (!TryComp<PdaComponent>(pdaUid, out var pda))
        {
            _sawmill.Warning($"PDA component not found: {ToPrettyString(pdaUid)}");
            return;
        }

        var userId = args.SenderAddress;
        var userName = pda.OwnerName ?? Loc.GetString("messenger-user-unknown");

        string? jobTitle = null;
        string? departmentId = null;

        if (pda.ContainedId != null && TryComp<IdCardComponent>(pda.ContainedId.Value, out var idCard))
        {
            jobTitle = idCard.LocalizedJobTitle;
            if (idCard.JobDepartments.Count > 0)
            {
                departmentId = idCard.JobDepartments[0];
            }
        }

        var user = new MessengerUser(userId, userName, jobTitle, departmentId);
        component.Users[userId] = user;

        AddUserToAutoGroups(uid, component, userId, userName, departmentId);

        var response = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdUserRegistered,
            ["user_id"] = userId,
            ["user_name"] = userName,
            ["job_title"] = jobTitle ?? string.Empty,
            ["department_id"] = departmentId ?? string.Empty
        };

        if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
        {
            _sawmill.Warning($"Server does not have DeviceNetworkComponent: {ToPrettyString(uid)}");
            return;
        }

        uint? pdaFrequency;
        if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var pdaFreq))
        {
            pdaFrequency = pdaFreq.Frequency;
        }
        else
        {
            _sawmill.Error($"PDA frequency prototype not found");
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(serverDevice.DeviceNetId, args.SenderAddress))
        {
            return;
        }

        _deviceNetwork.QueuePacket(uid, args.SenderAddress, response, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
    }

    private void HandleSendMessage(EntityUid uid, MessengerServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(CmdSendMessage, out NetworkPayload? messageData))
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
            [DeviceNetworkConstants.Command] = CmdMessageReceived,
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
                [DeviceNetworkConstants.Command] = CmdMessagesList,
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
            [DeviceNetworkConstants.Command] = CmdMessageReceived,
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

    private void HandleCreateGroup(EntityUid uid, MessengerServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(CmdCreateGroup, out NetworkPayload? groupData))
            return;

        if (!groupData.TryGetValue("name", out string? groupName) || string.IsNullOrWhiteSpace(groupName))
            return;

        if (!component.Users.TryGetValue(args.SenderAddress, out var creator))
            return;

        var groupId = $"group_{++component.GroupIdCounter}";
        var members = new HashSet<string> { creator.UserId };

        var group = new MessengerGroup(groupId, groupName, members, MessengerGroupType.UserCreated, null, creator.UserId);
        component.Groups[groupId] = group;

        if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
            return;

        uint? pdaFrequency = null;
        if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var pdaFreq))
        {
            pdaFrequency = pdaFreq.Frequency;
        }

        var membersList = new List<object>();
        foreach (var memberId in group.Members)
        {
            membersList.Add(memberId);
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdGroupCreated,
            ["group_id"] = group.GroupId,
            ["group_name"] = group.Name,
            ["group_type"] = (int)group.Type,
            ["auto_group_prototype_id"] = group.AutoGroupPrototypeId ?? string.Empty,
            ["owner_id"] = group.OwnerId ?? string.Empty,
            ["members"] = membersList
        };

        if (pdaFrequency.HasValue)
        {
            _deviceNetwork.QueuePacket(uid, args.SenderAddress, payload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
        }
        else
        {
            _deviceNetwork.QueuePacket(uid, args.SenderAddress, payload);
        }
    }

    private void HandleAddToGroup(EntityUid uid, MessengerServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(CmdAddToGroup, out NetworkPayload? addData))
            return;

        if (!addData.TryGetValue("group_id", out string? groupId))
            return;

        if (!addData.TryGetValue("user_id", out string? userId))
            return;

        if (!component.Groups.TryGetValue(groupId, out var group))
            return;

        if (group.Type != MessengerGroupType.UserCreated)
        {
            if (group.AutoGroupPrototypeId != null)
            {
                if (_prototypeManager.TryIndex<MessengerAutoGroupPrototype>(group.AutoGroupPrototypeId, out var autoGroupProto))
                {
                    if (!autoGroupProto.AllowManualMemberManagement)
                        return;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        if (!component.Users.TryGetValue(args.SenderAddress, out var adder))
            return;

        if (group.OwnerId != adder.UserId)
            return;

        if (!component.Users.TryGetValue(userId, out _))
            return;

        if (group.Members.Contains(userId))
            return;

        group.Members.Add(userId);

        if (!component.Users.TryGetValue(userId, out var addedUser))
            return;

        var timestamp = GetStationTime();
        var messageText = Loc.GetString("messenger-system-user-added-by", ("adderName", adder.Name), ("userName", addedUser.Name));
        var systemMessage = new MessengerMessage("system", Loc.GetString("messenger-system-name"), messageText, timestamp, groupId);

        if (!component.MessageHistory.TryGetValue(groupId, out var history))
        {
            history = new List<MessengerMessage>();
            component.MessageHistory[groupId] = history;
        }
        history.Add(systemMessage);
        TrimMessageHistory(history, component.MaxMessageHistory);

        uint? pdaFrequency = null;
        if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var pdaFreq))
        {
            pdaFrequency = pdaFreq.Frequency;
        }

        if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
            return;

        var messagePayload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdMessageReceived,
            ["sender_id"] = "system",
            ["sender_name"] = Loc.GetString("messenger-system-name"),
            ["content"] = messageText,
            ["timestamp"] = timestamp.TotalSeconds,
            ["group_id"] = groupId,
            ["recipient_id"] = string.Empty,
            ["is_read"] = false
        };

        foreach (var memberId in group.Members)
        {
            if (pdaFrequency.HasValue)
            {
                _deviceNetwork.QueuePacket(uid, memberId, messagePayload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
            }
            else
            {
                _deviceNetwork.QueuePacket(uid, memberId, messagePayload);
            }
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdUserAddedToGroup,
            ["group_id"] = groupId,
            ["user_id"] = userId
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

    private void HandleRemoveFromGroup(EntityUid uid, MessengerServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(CmdRemoveFromGroup, out NetworkPayload? removeData))
            return;

        if (!removeData.TryGetValue("group_id", out string? groupId))
            return;

        if (!removeData.TryGetValue("user_id", out string? userId))
            return;

        if (!component.Groups.TryGetValue(groupId, out var group))
            return;

        if (group.Type != MessengerGroupType.UserCreated)
        {
            if (group.AutoGroupPrototypeId != null)
            {
                if (_prototypeManager.TryIndex<MessengerAutoGroupPrototype>(group.AutoGroupPrototypeId, out var autoGroupProto))
                {
                    if (!autoGroupProto.AllowManualMemberManagement)
                        return;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        if (!component.Users.TryGetValue(args.SenderAddress, out var remover))
            return;

        if (group.OwnerId != remover.UserId)
            return;

        if (userId == group.OwnerId)
            return;

        if (!group.Members.Contains(userId))
            return;

        if (!component.Users.TryGetValue(userId, out var removedUser))
            return;

        group.Members.Remove(userId);

        var timestamp = GetStationTime();
        var messageText = Loc.GetString("messenger-system-user-removed-by", ("removerName", remover.Name), ("userName", removedUser.Name));
        var systemMessage = new MessengerMessage("system", Loc.GetString("messenger-system-name"), messageText, timestamp, groupId);

        if (!component.MessageHistory.TryGetValue(groupId, out var history))
        {
            history = new List<MessengerMessage>();
            component.MessageHistory[groupId] = history;
        }
        history.Add(systemMessage);
        TrimMessageHistory(history, component.MaxMessageHistory);

        uint? pdaFrequency = null;
        if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var pdaFreq))
        {
            pdaFrequency = pdaFreq.Frequency;
        }

        if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
            return;

        var messagePayload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdMessageReceived,
            ["sender_id"] = "system",
            ["sender_name"] = Loc.GetString("messenger-system-name"),
            ["content"] = messageText,
            ["timestamp"] = timestamp.TotalSeconds,
            ["group_id"] = groupId,
            ["recipient_id"] = string.Empty,
            ["is_read"] = false
        };

        foreach (var memberId in group.Members)
        {
            if (pdaFrequency.HasValue)
            {
                _deviceNetwork.QueuePacket(uid, memberId, messagePayload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
            }
            else
            {
                _deviceNetwork.QueuePacket(uid, memberId, messagePayload);
            }
        }
    }

    private void HandleGetUsers(EntityUid uid, MessengerServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
            return;

        var usersList = component.Users.Values.ToList();
        var usersData = new List<Dictionary<string, object>>();

        foreach (var user in usersList)
        {
            usersData.Add(new Dictionary<string, object>
            {
                ["user_id"] = user.UserId,
                ["user_name"] = user.Name,
                ["job_title"] = user.JobTitle ?? string.Empty,
                ["department_id"] = user.DepartmentId ?? string.Empty
            });
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdUsersList,
            ["users"] = usersData
        };

        uint? pdaFrequency = null;
        if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var pdaFreq))
        {
            pdaFrequency = pdaFreq.Frequency;
        }

        if (pdaFrequency.HasValue)
        {
            _deviceNetwork.QueuePacket(uid, args.SenderAddress, payload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
        }
        else
        {
            _deviceNetwork.QueuePacket(uid, args.SenderAddress, payload);
        }
    }

    private void HandleGetGroups(EntityUid uid, MessengerServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
            return;

        var senderUserId = args.SenderAddress;
        if (string.IsNullOrEmpty(senderUserId))
            return;

        var groupsList = component.Groups.Values.ToList();
        var groupsData = new List<Dictionary<string, object>>();

        foreach (var group in groupsList)
        {
            if (group.GroupId == "common")
            {
                if (!group.Members.Contains(senderUserId))
                    continue;
            }
            else if (group.GroupId.StartsWith("dept_"))
            {
                if (!group.Members.Contains(senderUserId))
                    continue;
            }
            else if (group.GroupId.StartsWith("group_"))
            {
                if (!group.Members.Contains(senderUserId))
                    continue;
            }

            var membersList = new List<object>();
            foreach (var memberId in group.Members)
            {
                membersList.Add(memberId);
            }

            var unreadCount = 0;
            if (component.UnreadCounts.TryGetValue(senderUserId, out var userUnreads))
            {
                userUnreads.TryGetValue(group.GroupId, out unreadCount);
            }

            groupsData.Add(new Dictionary<string, object>
            {
            ["group_id"] = group.GroupId,
            ["group_name"] = group.Name,
            ["group_type"] = (int)group.Type,
            ["auto_group_prototype_id"] = group.AutoGroupPrototypeId ?? string.Empty,
            ["owner_id"] = group.OwnerId ?? string.Empty,
            ["members"] = membersList,
            ["unread_count"] = unreadCount
            });
        }

        var unreadCountsData = new Dictionary<string, int>();
        if (component.UnreadCounts.TryGetValue(senderUserId, out var senderUnreads))
        {
            foreach (var (chatId, count) in senderUnreads)
            {
                unreadCountsData[chatId] = count;
            }
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdGroupsList,
            ["groups"] = groupsData,
            ["unread_counts"] = unreadCountsData
        };

        uint? pdaFrequency = null;
        if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var pdaFreq))
        {
            pdaFrequency = pdaFreq.Frequency;
        }

        if (pdaFrequency.HasValue)
        {
            _deviceNetwork.QueuePacket(uid, args.SenderAddress, payload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
        }
        else
        {
            _deviceNetwork.QueuePacket(uid, args.SenderAddress, payload);
        }
    }

    private void HandleGetMessages(EntityUid uid, MessengerServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
            return;

        if (!args.Data.TryGetValue(CmdGetMessages, out NetworkPayload? messageRequest))
            return;

        if (!messageRequest.TryGetValue("chat_id", out string? chatId))
            return;

        var userId = args.SenderAddress;

        if (!string.IsNullOrEmpty(userId))
        {
            component.OpenChats[userId] = chatId;
        }

        if (!string.IsNullOrEmpty(userId) && component.UnreadCounts.TryGetValue(userId, out var userUnreads))
        {
            userUnreads.Remove(chatId);
        }

        if (!string.IsNullOrEmpty(userId) && component.MessageHistory.TryGetValue(chatId, out var chatMessages))
        {
            var updatedSenders = new HashSet<string>();
            var hasUpdates = false;
            foreach (var message in chatMessages)
            {
                if (message.RecipientId == userId && !string.IsNullOrEmpty(message.RecipientId) && !message.IsRead)
                {
                    message.IsRead = true;
                    hasUpdates = true;

                    if (!string.IsNullOrEmpty(message.SenderId) && message.SenderId != userId)
                    {
                        updatedSenders.Add(message.SenderId);
                    }
                }
            }

            if (updatedSenders.Count > 0)
            {
                uint? updatePdaFrequency = null;
                if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var updatePdaFreq))
                {
                    updatePdaFrequency = updatePdaFreq.Frequency;
                }

                foreach (var senderId in updatedSenders)
                {
                    var sortedChatMessages = chatMessages.OrderBy(m => m.Timestamp)
                        .ThenBy(m => m.SenderId)
                        .ThenBy(m => m.Content)
                        .ToList();

                    var senderMessagesData = new List<Dictionary<string, object>>();
                    foreach (var msg in sortedChatMessages)
                    {
                        if (msg.SenderId == senderId || msg.RecipientId == senderId)
                        {
                            senderMessagesData.Add(new Dictionary<string, object>
                            {
                                ["sender_id"] = msg.SenderId,
                                ["sender_name"] = msg.SenderName,
                                ["content"] = msg.Content,
                                ["timestamp"] = msg.Timestamp.TotalSeconds,
                                ["group_id"] = msg.GroupId ?? string.Empty,
                                ["recipient_id"] = msg.RecipientId ?? string.Empty,
                                ["is_read"] = msg.IsRead
                            });
                        }
                    }

                    var updatePayload = new NetworkPayload
                    {
                        [DeviceNetworkConstants.Command] = CmdMessagesList,
                        ["messages"] = senderMessagesData,
                        ["chat_id"] = chatId
                    };

                    if (updatePdaFrequency.HasValue)
                    {
                        _deviceNetwork.QueuePacket(uid, senderId, updatePayload, frequency: updatePdaFrequency, network: serverDevice.DeviceNetId);
                    }
                    else
                    {
                        _deviceNetwork.QueuePacket(uid, senderId, updatePayload);
                    }
                }
            }

            if (hasUpdates)
            {
                var sortedChatMessages = chatMessages.OrderBy(m => m.Timestamp)
                    .ThenBy(m => m.SenderId)
                    .ThenBy(m => m.Content)
                    .ToList();

                var recipientMessagesData = new List<Dictionary<string, object>>();
                foreach (var msg in sortedChatMessages)
                {
                    recipientMessagesData.Add(new Dictionary<string, object>
                    {
                        ["sender_id"] = msg.SenderId,
                        ["sender_name"] = msg.SenderName,
                        ["content"] = msg.Content,
                        ["timestamp"] = msg.Timestamp.TotalSeconds,
                        ["group_id"] = msg.GroupId ?? string.Empty,
                        ["recipient_id"] = msg.RecipientId ?? string.Empty,
                        ["is_read"] = msg.IsRead
                    });
                }

                var recipientPayload = new NetworkPayload
                {
                    [DeviceNetworkConstants.Command] = CmdMessagesList,
                    ["messages"] = recipientMessagesData,
                    ["chat_id"] = chatId
                };

                uint? recipientPdaFrequency = null;
                if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var recipientPdaFreq))
                {
                    recipientPdaFrequency = recipientPdaFreq.Frequency;
                }

                if (recipientPdaFrequency.HasValue)
                {
                    _deviceNetwork.QueuePacket(uid, userId, recipientPayload, frequency: recipientPdaFrequency, network: serverDevice.DeviceNetId);
                }
                else
                {
                    _deviceNetwork.QueuePacket(uid, userId, recipientPayload);
                }
            }
        }

        if (!component.MessageHistory.TryGetValue(chatId, out var messages))
            messages = new List<MessengerMessage>();

        var sortedMessages = messages.OrderBy(m => m.Timestamp)
            .ThenBy(m => m.SenderId)
            .ThenBy(m => m.Content)
            .ToList();

        var messagesData = new List<Dictionary<string, object>>();
        foreach (var message in sortedMessages)
        {
            messagesData.Add(new Dictionary<string, object>
            {
                ["sender_id"] = message.SenderId,
                ["sender_name"] = message.SenderName,
                ["content"] = message.Content,
                ["timestamp"] = message.Timestamp.TotalSeconds,
                ["group_id"] = message.GroupId ?? string.Empty,
                ["recipient_id"] = message.RecipientId ?? string.Empty,
                ["is_read"] = message.IsRead
            });
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdMessagesList,
            ["messages"] = messagesData,
            ["chat_id"] = chatId
        };

        uint? pdaFrequency = null;
        if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var pdaFreq))
        {
            pdaFrequency = pdaFreq.Frequency;
        }

        if (pdaFrequency.HasValue)
        {
            _deviceNetwork.QueuePacket(uid, args.SenderAddress, payload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
        }
        else
        {
            _deviceNetwork.QueuePacket(uid, args.SenderAddress, payload);
        }
    }

    /// <summary>
    /// Генерирует ID для личного чата между двумя пользователями
    /// </summary>
    private string GetPersonalChatId(string userId1, string userId2)
    {
        var ids = new[] { userId1, userId2 }.OrderBy(x => x).ToArray();
        return $"personal_{ids[0]}_{ids[1]}";
    }

    /// <summary>
    /// Ограничивает историю сообщений до указанного количества
    /// </summary>
    private void TrimMessageHistory(List<MessengerMessage> history, int maxCount)
    {
        if (history.Count > maxCount)
        {
            var toRemove = history.Count - maxCount;
            history.RemoveRange(0, toRemove);
        }
    }

    /// <summary>
    /// Получает время станции (обычное время, как в КПК)
    /// </summary>
    private TimeSpan GetStationTime()
    {
        return (DateTime.UtcNow + TimeSpan.FromHours(3)).TimeOfDay;
    }
}
