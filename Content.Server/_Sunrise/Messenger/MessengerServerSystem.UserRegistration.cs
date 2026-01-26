using Content.Server.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared._Sunrise.Messenger;
using Content.Shared.PDA;
using Content.Shared.Access.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.GameTicking;

namespace Content.Server._Sunrise.Messenger;

/// <summary>
/// Часть системы мессенджера, отвечающая за регистрацию пользователей
/// </summary>
public sealed partial class MessengerServerSystem
{
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
            Sawmill.Warning($"No PDA found for player: {ToPrettyString(args.Mob)}");
            return;
        }

        var station = _stationSystem.GetOwningStation(args.Mob);
        if (station == null)
        {
            Sawmill.Warning($"No station found for player: {ToPrettyString(args.Mob)}");
            return;
        }

        Sawmill.Debug($"Player station: {ToPrettyString(station.Value)}");

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
            Sawmill.Warning($"No messenger servers found on station: {ToPrettyString(station.Value)}");
        }

        if (serverUid == null || serverComponent == null || serverDevice == null)
        {
            Sawmill.Warning($"No active messenger server found for player: {ToPrettyString(args.Mob)}");
            return;
        }

        Sawmill.Debug($"Server DeviceNetwork before registration: Address={serverDevice.Address}, TransmitFrequency={serverDevice.TransmitFrequency}, ReceiveFrequency={serverDevice.ReceiveFrequency}");

        RegisterUserFromPda(serverUid.Value, serverComponent, pdaUid.Value);
    }

    /// <summary>
    /// Регистрирует пользователя на сервере мессенджера по его PDA
    /// </summary>
    private void RegisterUserFromPda(EntityUid uid, MessengerServerComponent component, EntityUid pdaUid)
    {
        if (!TryComp<PdaComponent>(pdaUid, out var pda))
        {
            Sawmill.Warning($"PDA component not found: {ToPrettyString(pdaUid)}");
            return;
        }

        if (!TryComp<DeviceNetworkComponent>(pdaUid, out var pdaDevice))
        {
            Sawmill.Warning($"DeviceNetwork component not found on PDA: {ToPrettyString(pdaUid)}");
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

    private void HandleRegisterUser(EntityUid uid, MessengerServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(MessengerCommands.CmdRegisterUser, out NetworkPayload? userData))
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
            Sawmill.Warning($"PDA component not found: {ToPrettyString(pdaUid)}");
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
            [DeviceNetworkConstants.Command] = MessengerCommands.CmdUserRegistered,
            ["user_id"] = userId,
            ["user_name"] = userName,
            ["job_title"] = jobTitle ?? string.Empty,
            ["department_id"] = departmentId ?? string.Empty
        };

        if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
        {
            Sawmill.Warning($"Server does not have DeviceNetworkComponent: {ToPrettyString(uid)}");
            return;
        }

        uint? pdaFrequency;
        if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var pdaFreq))
        {
            pdaFrequency = pdaFreq.Frequency;
        }
        else
        {
            Sawmill.Error($"PDA frequency prototype not found");
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(serverDevice.DeviceNetId, args.SenderAddress))
        {
            return;
        }

        _deviceNetwork.QueuePacket(uid, args.SenderAddress, response, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
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

        Sawmill.Info($"Created {created} automatic messenger groups");
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
                    [DeviceNetworkConstants.Command] = MessengerCommands.CmdMessageReceived,
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
                    [DeviceNetworkConstants.Command] = MessengerCommands.CmdUserAddedToGroup,
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
}
