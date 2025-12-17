using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.CrewMonitoring; // Sunrise - edit
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Timing;
using Content.Shared.DeviceNetwork.Components;

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringServerSystem : EntitySystem
{
    [Dependency] private readonly SuitSensorSystem _sensors = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!; // Sunrise - Added

    private const float UpdateRate = 3f;
    private float _updateDiff;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringServerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringServerComponent, DeviceNetServerDisconnectedEvent>(OnDisconnected);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // check update rate
        _updateDiff += frameTime;
        if (_updateDiff < UpdateRate)
            return;
        _updateDiff -= UpdateRate;

        var servers = EntityQueryEnumerator<CrewMonitoringServerComponent>();

        while (servers.MoveNext(out var id, out var server))
        {
            if (!_singletonServerSystem.IsActiveServer(id))
                continue;

            UpdateTimeout(id);
            BroadcastSensorStatus(id, server);
        }
    }

    /// <summary>
    /// Adds or updates a sensor status entry if the received package is a sensor status update
    /// </summary>
    private void OnPacketReceived(EntityUid uid, CrewMonitoringServerComponent component, DeviceNetworkPacketEvent args)
    {
        var sensorStatus = _sensors.PacketToSuitSensor(args.Data);
        if (sensorStatus == null)
            return;

        var serverTransform = Transform(uid);
        var serverMapId = serverTransform.MapID;

        if (sensorStatus.MapId != serverMapId)
            return;

        sensorStatus.Timestamp = _gameTiming.CurTime;
        component.SensorStatus[args.SenderAddress] = sensorStatus;
    }

    /// <summary>
    /// Clears the servers sensor status list
    /// </summary>
    private void OnRemove(EntityUid uid, CrewMonitoringServerComponent component, ComponentRemove args)
    {
        component.SensorStatus.Clear();
    }

    /// <summary>
    /// Drop the sensor status if it hasn't been updated for to long
    /// </summary>
    private void UpdateTimeout(EntityUid uid, CrewMonitoringServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // Sunrise - Start
        // Удаление из Dictionary во время foreach приводит к исключению, поэтому собираем ключики отдельно
        // Вероятно можно улучшить код
        var timeout = TimeSpan.FromSeconds(component.SensorTimeout);
        List<string>? toRemove = null;

        foreach (var (address, sensor) in component.SensorStatus)
        {
            var dif = _gameTiming.CurTime - sensor.Timestamp;
            if (dif <= timeout)
                continue;

            toRemove ??= new List<string>();
            toRemove.Add(address);
        }

        if (toRemove == null)
            return;

        foreach (var address in toRemove)
            component.SensorStatus.Remove(address);
        // Sunrise - End
    }

    /// <summary>
    /// Broadcasts the status of all connected sensors
    /// </summary>
    private void BroadcastSensorStatus(EntityUid uid, CrewMonitoringServerComponent? serverComponent = null, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(uid, ref serverComponent, ref device))
            return;

        var serverTransform = Transform(uid);
        var serverMapId = serverTransform.MapID;
        // Sunrise - Start
        var serverGrid = serverTransform.GridUid;
        var serverPos = _transformSystem.GetWorldPosition(serverTransform);
        var rangeSquared = serverComponent.MonitoringRange * serverComponent.MonitoringRange;

        // Фильтр только на гриде сервера и только в радиусу
        var filteredStatus = new Dictionary<string, SuitSensorStatus>(serverComponent.SensorStatus.Count);
        foreach (var (address, status) in serverComponent.SensorStatus)
        {
            var owner = GetEntity(status.OwnerUid);
            if (!EntityManager.EntityExists(owner))
                continue;

            var ownerXform = Transform(owner);
            if (ownerXform.MapID != serverMapId)
                continue;

            // Сенсор работает в космосе пока в радиусе сервера мониторинга
            if (serverGrid != null && ownerXform.GridUid != null && ownerXform.GridUid != serverGrid)
                continue;

            var ownerPos = _transformSystem.GetWorldPosition(ownerXform);
            if ((serverPos - ownerPos).LengthSquared() > rangeSquared)
                continue;

            filteredStatus[address] = status;
        }
        // Sunrise - End

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdUpdatedState,
            [SuitSensorConstants.NET_STATUS_COLLECTION] = filteredStatus, // Sunrise - Added
            [SuitSensorConstants.MAP_ID] = serverMapId,
        };
        // Sunrise - Start
        if (serverGrid != null)
            payload[CrewMonitoringNetKeys.MonitoringGrid] = GetNetEntity(serverGrid.Value);
        // Sunrise - End

        _deviceNetworkSystem.QueuePacket(uid, null, payload, device: device);
    }

    /// <summary>
    /// Clears sensor data on disconnect
    /// </summary>
    private void OnDisconnected(EntityUid uid, CrewMonitoringServerComponent component, ref DeviceNetServerDisconnectedEvent _)
    {
        component.SensorStatus.Clear();
    }
}
