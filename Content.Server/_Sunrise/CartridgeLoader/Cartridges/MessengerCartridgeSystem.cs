using System.Linq;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Messenger;
using Content.Server.PDA.Ringer;
using Content.Server.Station.Systems;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Messenger;
using Content.Shared.PDA.Ringer;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Server.CartridgeLoader.Cartridges;

/// <summary>
/// Система картриджа мессенджера для КПК
/// </summary>
public sealed class MessengerCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServer = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly RingerSystem _ringer = default!;

    private ISawmill _sawmill = default!;
    private const string MessengerFrequencyId = "Messenger";

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

        _sawmill = _logManager.GetSawmill("messenger.cartridge");

        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeActivatedEvent>(OnCartridgeActivated);
        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeAddedEvent>(OnCartridgeAdded);
        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeDeviceNetPacketEvent>(OnPacketReceived);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MessengerCartridgeComponent>();
        var currentTime = _gameTiming.CurTime;

        while (query.MoveNext(out var uid, out var component))
        {
            if (component.LoaderUid == null)
                continue;

            if (component.LastStatusCheck.HasValue)
            {
                var timeSinceLastCheck = currentTime - component.LastStatusCheck.Value;
                if (timeSinceLastCheck.TotalSeconds < 2.0)
                    continue;
            }

            component.LastStatusCheck = currentTime;

            CheckServerStatus(uid, component, component.LoaderUid.Value);
        }
    }

    private void CheckServerStatus(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid)
    {
        if (!TryGetPdaAndDeviceNetwork(loaderUid, out var pdaUid, out _))
            return;

        var station = _stationSystem.GetOwningStation(pdaUid);
        if (station == null)
        {
            component.ServerAddress = null;
            component.IsRegistered = false;
            component.LastRegistrationAttempt = null;
            UpdateUiState(uid, loaderUid, component);
            return;
        }

        if (!_singletonServer.TryGetActiveServerAddress<MessengerServerComponent>(station.Value, out var serverAddress))
        {
            component.ServerAddress = null;
            component.IsRegistered = false;
            component.LastRegistrationAttempt = null;
            UpdateUiState(uid, loaderUid, component);
            return;
        }

        if (component.ServerAddress != serverAddress)
        {
            component.ServerAddress = serverAddress;
            component.IsRegistered = false;
            component.UserId = null;
            component.LastRegistrationAttempt = null;
            if (TryGetPdaAndDeviceNetwork(loaderUid, out _, out var deviceNetwork))
            {
                TryConnectToServer(uid, component, loaderUid);
            }
            UpdateUiState(uid, loaderUid, component);
        }
        else if (component.ServerAddress == null)
        {
            component.IsRegistered = false;
            component.UserId = null;
            component.LastRegistrationAttempt = null;
            UpdateUiState(uid, loaderUid, component);
        }
        else if (!component.IsRegistered)
        {
            if (TryGetPdaAndDeviceNetwork(loaderUid, out _, out var deviceNetwork))
            {
                TryConnectToServer(uid, component, loaderUid);
            }
            UpdateUiState(uid, loaderUid, component);
        }
        else
        {
            var currentTime = _gameTiming.CurTime;
            if (!component.LastUsersUpdate.HasValue ||
                (currentTime - component.LastUsersUpdate.Value).TotalSeconds >= 10.0)
            {
                component.LastUsersUpdate = currentTime;
                if (TryGetPdaAndDeviceNetwork(loaderUid, out _, out var deviceNetwork))
                {
                    RequestUsers(uid, component, loaderUid, deviceNetwork);
                    RequestGroups(uid, component, loaderUid, deviceNetwork);
                }
            }
            UpdateUiState(uid, loaderUid, component);
        }
    }

    private void OnCartridgeActivated(EntityUid uid, MessengerCartridgeComponent component, CartridgeActivatedEvent args)
    {
        TryConnectToServer(uid, component, args.Loader);
    }

    private void OnCartridgeAdded(EntityUid uid, MessengerCartridgeComponent component, CartridgeAddedEvent args)
    {
        component.LoaderUid = args.Loader;
        TryConnectToServer(uid, component, args.Loader);

        _cartridgeLoader.RegisterBackgroundProgram(args.Loader, uid);
    }

    private void OnUiReady(EntityUid uid, MessengerCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        if (!component.IsRegistered && component.ServerAddress == null)
        {
            TryConnectToServer(uid, component, args.Loader);
        }
        else
        {
            UpdateUiState(uid, args.Loader, component);
        }
    }

    private void OnUiMessage(EntityUid uid, MessengerCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is not MessengerUiMessageEvent message)
            return;

        var loaderUid = GetEntity(args.LoaderUid);
        if (!TryGetPdaAndDeviceNetwork(loaderUid, out var pdaUid, out var deviceNetwork))
            return;

        switch (message.Action)
        {
            case MessengerUiAction.SendMessage:
                if (message.Content != null)
                    SendMessage(uid, component, loaderUid, deviceNetwork, message.RecipientId, message.GroupId, message.Content);
                break;
            case MessengerUiAction.CreateGroup:
                if (message.GroupName != null)
                    CreateGroup(uid, component, loaderUid, deviceNetwork, message.GroupName);
                break;
            case MessengerUiAction.AddToGroup:
                if (message.GroupId != null && message.UserId != null)
                    AddToGroup(uid, component, loaderUid, deviceNetwork, message.GroupId, message.UserId);
                break;
            case MessengerUiAction.RemoveFromGroup:
                if (message.GroupId != null && message.UserId != null)
                    RemoveFromGroup(uid, component, loaderUid, deviceNetwork, message.GroupId, message.UserId);
                break;
            case MessengerUiAction.RequestUsers:
                RequestUsers(uid, component, loaderUid, deviceNetwork);
                break;
            case MessengerUiAction.RequestGroups:
                RequestGroups(uid, component, loaderUid, deviceNetwork);
                break;
            case MessengerUiAction.RequestMessages:
                if (message.ChatId != null)
                    RequestMessages(uid, component, loaderUid, deviceNetwork, message.ChatId);
                break;
            case MessengerUiAction.ToggleMute:
                if (message.ChatId != null && message.IsMuted.HasValue)
                    ToggleMute(uid, component, message.ChatId, message.IsMuted.Value);
                break;
        }
    }

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
            _sawmill.Warning($"Packet received but LoaderUid is invalid");
            return;
        }

        switch (command)
        {
            case CmdUserRegistered:
                HandleUserRegistered(uid, component, packet, loaderUid);
                break;
            case CmdUsersList:
                HandleUsersList(uid, component, packet, loaderUid);
                break;
            case CmdGroupsList:
                HandleGroupsList(uid, component, packet, loaderUid);
                break;
            case CmdMessagesList:
                HandleMessagesList(uid, component, packet, loaderUid);
                break;
            case CmdMessageReceived:
                HandleMessageReceived(uid, component, packet, loaderUid);
                break;
            case CmdGroupCreated:
                HandleGroupCreated(uid, component, packet, loaderUid);
                break;
            case CmdUserAddedToGroup:
                HandleUserAddedToGroup(uid, component, packet, loaderUid);
                break;
            default:
                _sawmill.Warning($"Unknown command received: {command}");
                break;
        }
    }

    private bool TryGetPdaAndDeviceNetwork(EntityUid loaderUid, out EntityUid pdaUid, out DeviceNetworkComponent deviceNetwork)
    {
        pdaUid = EntityUid.Invalid;
        deviceNetwork = null!;

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var device))
            return false;

        pdaUid = loaderUid;
        deviceNetwork = device;
        return true;
    }

    private EntityUid GetEntity(NetEntity netEntity)
    {
        return EntityManager.GetEntity(netEntity);
    }

    private void TryConnectToServer(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid)
    {

        if (!TryGetPdaAndDeviceNetwork(loaderUid, out var pdaUid, out var deviceNetwork))
        {
            _sawmill.Warning($"Failed to get PDA and DeviceNetwork: {ToPrettyString(loaderUid)}");
            return;
        }

        component.LoaderUid = loaderUid;

        var station = _stationSystem.GetOwningStation(pdaUid);
        if (station == null)
        {
            _sawmill.Warning($"No station found for PDA: {ToPrettyString(pdaUid)}");
            component.ServerAddress = null;
            component.IsRegistered = false;
            UpdateUiState(uid, loaderUid, component);
            return;
        }

        if (!_singletonServer.TryGetActiveServerAddress<MessengerServerComponent>(station.Value, out var serverAddress))
        {
            _sawmill.Warning($"No active messenger server found on station: {ToPrettyString(station.Value)}");
            component.ServerAddress = null;
            component.IsRegistered = false;
            UpdateUiState(uid, loaderUid, component);
            return;
        }

        if (string.IsNullOrEmpty(serverAddress))
        {
            _sawmill.Warning($"Server address is empty, server may not be connected to DeviceNetwork yet");
            component.ServerAddress = null;
            component.IsRegistered = false;
            UpdateUiState(uid, loaderUid, component);
            return;
        }

        _sawmill.Debug($"Found active server address: {serverAddress}");

        if (component.ServerAddress != serverAddress)
        {
            component.ServerAddress = serverAddress;
            component.IsRegistered = false;
            component.UserId = null;
            component.LastRegistrationAttempt = null;
        }

        if (component.IsRegistered && component.ServerAddress == serverAddress)
        {
            _sawmill.Debug($"Already registered, updating UI state");
            UpdateUiState(uid, loaderUid, component);
            return;
        }

        if (!component.IsRegistered)
        {
            RegisterUser(uid, component, loaderUid, deviceNetwork, pdaUid);
        }
    }

    private void RegisterUser(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork, EntityUid pdaUid)
    {
        if (component.ServerAddress == null)
        {
            _sawmill.Warning($"Cannot register: ServerAddress is null");
            return;
        }

        var currentTime = _gameTiming.CurTime;
        if (component.LastRegistrationAttempt.HasValue)
        {
            var timeSinceLastAttempt = currentTime - component.LastRegistrationAttempt.Value;
            if (timeSinceLastAttempt.TotalSeconds < 5.0)
            {
                _sawmill.Debug($"Registration attempt too soon, waiting: {timeSinceLastAttempt.TotalSeconds:F2}s");
                return;
            }
        }

        component.LastRegistrationAttempt = currentTime;

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdRegisterUser,
            [CmdRegisterUser] = new NetworkPayload
            {
                ["pda_uid"] = GetNetEntity(pdaUid)
            }
        };

        uint? messengerFrequency;
        if (_prototypeManager.TryIndex<DeviceFrequencyPrototype>(MessengerFrequencyId, out var messengerFreq))
        {
            messengerFrequency = messengerFreq.Frequency;
        }
        else
        {
            _sawmill.Error($"Messenger frequency prototype not found: {MessengerFrequencyId}");
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(deviceNetwork.DeviceNetId, component.ServerAddress))
        {
            _sawmill.Warning($"Server address {component.ServerAddress} is not present in network {deviceNetwork.DeviceNetId}");
            return;
        }

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            _sawmill.Error($"Failed to get DeviceNetworkComponent after setting frequency");
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        _sawmill.Debug($"PDA DeviceNetwork: Address={pdaDevice.Address}, TransmitFrequency={pdaDevice.TransmitFrequency}, ReceiveFrequency={pdaDevice.ReceiveFrequency}, DeviceNetId={pdaDevice.DeviceNetId}");

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            _sawmill.Warning($"Server address {component.ServerAddress} is not present in network {pdaDevice.DeviceNetId} (PDA network)");
        }
        else
        {
            _sawmill.Debug($"Server address {component.ServerAddress} found in network {pdaDevice.DeviceNetId}");
        }

        var pdaTransform = Transform(loaderUid);
        var pdaPos = _transformSystem.GetWorldPosition(pdaTransform);
        _sawmill.Debug($"PDA position: {pdaPos}, MapId: {pdaTransform.MapID}");

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFrequency, network: pdaDevice.DeviceNetId);

        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }

    private void SendMessage(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork, string? recipientId, string? groupId, string content)
    {
        if (component.ServerAddress == null || !component.IsRegistered)
            return;

        var messengerFreq = GetMessengerFrequency();
        if (!messengerFreq.HasValue)
            return;

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            RestoreFrequency(loaderUid, pdaDevice, originalFreq);
            return;
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdSendMessage,
            [CmdSendMessage] = new NetworkPayload
            {
                ["content"] = content,
                ["recipient_id"] = recipientId ?? string.Empty,
                ["group_id"] = groupId ?? string.Empty
            }
        };

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFreq, network: pdaDevice.DeviceNetId);

        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }

    private void CreateGroup(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork, string groupName)
    {
        if (component.ServerAddress == null || !component.IsRegistered)
            return;

        var messengerFreq = GetMessengerFrequency();
        if (!messengerFreq.HasValue)
            return;

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            RestoreFrequency(loaderUid, pdaDevice, originalFreq);
            return;
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdCreateGroup,
            [CmdCreateGroup] = new NetworkPayload
            {
                ["name"] = groupName
            }
        };

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFreq, network: pdaDevice.DeviceNetId);
        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }

    private void AddToGroup(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork, string groupId, string userId)
    {
        if (component.ServerAddress == null || !component.IsRegistered)
            return;

        var messengerFreq = GetMessengerFrequency();
        if (!messengerFreq.HasValue)
            return;

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdAddToGroup,
            [CmdAddToGroup] = new NetworkPayload
            {
                ["group_id"] = groupId,
                ["user_id"] = userId
            }
        };

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            RestoreFrequency(loaderUid, pdaDevice, originalFreq);
            return;
        }

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFreq, network: pdaDevice.DeviceNetId);
        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }

    private void RemoveFromGroup(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork, string groupId, string userId)
    {
        if (component.ServerAddress == null || !component.IsRegistered)
            return;

        var messengerFreq = GetMessengerFrequency();
        if (!messengerFreq.HasValue)
            return;

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdRemoveFromGroup,
            [CmdRemoveFromGroup] = new NetworkPayload
            {
                ["group_id"] = groupId,
                ["user_id"] = userId
            }
        };

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            RestoreFrequency(loaderUid, pdaDevice, originalFreq);
            return;
        }

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFreq, network: pdaDevice.DeviceNetId);
        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }

    private void RequestUsers(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork)
    {
        if (component.ServerAddress == null)
            return;

        var messengerFreq = GetMessengerFrequency();
        if (!messengerFreq.HasValue)
            return;

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            RestoreFrequency(loaderUid, pdaDevice, originalFreq);
            return;
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdGetUsers
        };

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFreq, network: pdaDevice.DeviceNetId);
        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }

    private void RequestGroups(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork)
    {
        if (component.ServerAddress == null)
            return;

        var messengerFreq = GetMessengerFrequency();
        if (!messengerFreq.HasValue)
            return;

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            RestoreFrequency(loaderUid, pdaDevice, originalFreq);
            return;
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdGetGroups
        };

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFreq, network: pdaDevice.DeviceNetId);
        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }

    private void RequestMessages(EntityUid uid, MessengerCartridgeComponent component, EntityUid loaderUid, DeviceNetworkComponent deviceNetwork, string chatId)
    {
        if (component.ServerAddress == null)
            return;

        component.LastRequestedChatId = chatId;

        component.ServerUnreadCounts.Remove(chatId);

        var messengerFreq = GetMessengerFrequency();
        if (!messengerFreq.HasValue)
            return;

        SetMessengerFrequency(loaderUid, deviceNetwork, out var originalFreq);

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
        {
            RestoreFrequency(loaderUid, deviceNetwork, originalFreq);
            return;
        }

        if (!_deviceNetwork.IsAddressPresent(pdaDevice.DeviceNetId, component.ServerAddress))
        {
            RestoreFrequency(loaderUid, pdaDevice, originalFreq);
            return;
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = CmdGetMessages,
            [CmdGetMessages] = new NetworkPayload
            {
                ["chat_id"] = chatId
            }
        };

        _deviceNetwork.QueuePacket(loaderUid, component.ServerAddress, payload, frequency: messengerFreq, network: pdaDevice.DeviceNetId);
        RestoreFrequency(loaderUid, pdaDevice, originalFreq);
    }

    /// <summary>
    /// Получает частоту Messenger
    /// </summary>
    private uint? GetMessengerFrequency()
    {
        if (_prototypeManager.TryIndex<DeviceFrequencyPrototype>(MessengerFrequencyId, out var messengerFrequency))
        {
            return messengerFrequency.Frequency;
        }
        _sawmill.Error($"Messenger frequency prototype not found: {MessengerFrequencyId}");
        return null;
    }

    /// <summary>
    /// Устанавливает частоту передачи на Messenger
    /// </summary>
    private void SetMessengerFrequency(EntityUid loaderUid, DeviceNetworkComponent deviceNetwork, out uint? originalFrequency)
    {
        originalFrequency = deviceNetwork.TransmitFrequency;
        var messengerFreq = GetMessengerFrequency();
        if (messengerFreq.HasValue)
        {
            _deviceNetwork.SetTransmitFrequency(loaderUid, messengerFreq.Value, deviceNetwork);
        }
    }

    /// <summary>
    /// Восстанавливает исходную частоту передачи
    /// </summary>
    private void RestoreFrequency(EntityUid loaderUid, DeviceNetworkComponent deviceNetwork, uint? originalFrequency)
    {
        if (originalFrequency.HasValue)
        {
            _deviceNetwork.SetTransmitFrequency(loaderUid, originalFrequency.Value, deviceNetwork);
        }
    }

    private void HandleUserRegistered(EntityUid uid, MessengerCartridgeComponent component, DeviceNetworkPacketEvent packet, EntityUid loaderUid)
    {
        if (!packet.Data.TryGetValue("user_id", out string? userId))
        {
            _sawmill.Warning($"UserRegistered packet missing user_id");
            return;
        }

        _sawmill.Info($"User registered successfully: {userId}");
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

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, MessengerCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var unreadCounts = new Dictionary<string, int>();
        if (component.ServerAddress != null && component.UserId != null)
        {
            foreach (var (chatId, messages) in component.MessageHistory)
            {
                if (messages == null || messages.Count == 0)
                    continue;

                if (chatId.StartsWith("personal_"))
                {
                    var unreadCount = messages.Count(m => !m.IsRead && m.RecipientId == component.UserId && !string.IsNullOrEmpty(m.RecipientId));

                    if (component.ServerUnreadCounts.TryGetValue(chatId, out var serverCount) && serverCount > unreadCount)
                    {
                        unreadCount = serverCount;
                    }

                    if (unreadCount > 0)
                    {
                        unreadCounts[chatId] = unreadCount;
                    }
                }
            }

            foreach (var (chatId, count) in component.ServerUnreadCounts)
            {
                if (!chatId.StartsWith("personal_") && count > 0)
                {
                    unreadCounts[chatId] = count;
                }
            }
        }

        var state = new MessengerUiState(
            component.IsRegistered,
            component.ServerAddress != null,
            component.UserId,
            component.Users,
            component.Groups,
            component.MessageHistory,
            component.MutedPersonalChats,
            component.MutedGroupChats,
            unreadCounts
        );

        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
    }

    private void ToggleMute(EntityUid uid, MessengerCartridgeComponent component, string chatId, bool isMuted)
    {
        var isGroup = component.Groups.Any(g => g.GroupId == chatId);

        if (isGroup)
        {
            if (isMuted)
                component.MutedGroupChats.Add(chatId);
            else
                component.MutedGroupChats.Remove(chatId);
        }
        else
        {
            if (isMuted)
                component.MutedPersonalChats.Add(chatId);
            else
                component.MutedPersonalChats.Remove(chatId);
        }

        if (component.LoaderUid.HasValue)
            UpdateUiState(uid, component.LoaderUid.Value, component);
    }
}
