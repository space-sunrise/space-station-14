using System.Linq;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork.Events;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared._Sunrise.Messenger;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Messenger;

/// <summary>
/// Система сервера мессенджера, обрабатывающая сообщения между КПК
/// </summary>
public sealed partial class MessengerServerSystem : EntitySystem
{
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServer = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private ISawmill Sawmill { get; set; } = default!;

    public override void Initialize()
    {
        base.Initialize();

        Sawmill = _logManager.GetSawmill("messenger.server");

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
        Sawmill.Info($"Messenger server connected: {ToPrettyString(uid)}");

        if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
        {
            Sawmill.Error($"Server DeviceNetwork component not found: {ToPrettyString(uid)}");
            return;
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
            case MessengerCommands.CmdRegisterUser:
                HandleRegisterUser(uid, component, args);
                break;
            case MessengerCommands.CmdSendMessage:
                HandleSendMessage(uid, component, args);
                break;
            case MessengerCommands.CmdCreateGroup:
                HandleCreateGroup(uid, component, args);
                break;
            case MessengerCommands.CmdAddToGroup:
                HandleAddToGroup(uid, component, args);
                break;
            case MessengerCommands.CmdRemoveFromGroup:
                HandleRemoveFromGroup(uid, component, args);
                break;
            case MessengerCommands.CmdGetUsers:
                HandleGetUsers(uid, component, args);
                break;
            case MessengerCommands.CmdGetGroups:
                HandleGetGroups(uid, component, args);
                break;
            case MessengerCommands.CmdGetMessages:
                HandleGetMessages(uid, component, args);
                break;
            default:
                Sawmill.Warning($"Unknown command received: {command} from {args.SenderAddress}");
                break;
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
