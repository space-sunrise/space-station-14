using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.CrewMonitoring; // Sunrise - edit
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Timing;
using Content.Shared.DeviceNetwork.Components;
using Content.Server.Power.EntitySystems; // Sunrise - Edit

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringServerSystem : EntitySystem
{
    [Dependency] private readonly SuitSensorSystem _sensors = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    private const float UpdateRate = 3f;
    private float _updateDiff;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringServerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateDiff += frameTime;
        if (_updateDiff < UpdateRate)
            return;
        _updateDiff -= UpdateRate;

        var now = _gameTiming.CurTime; // Sunrise - Add
        var servers = EntityQueryEnumerator<CrewMonitoringServerComponent>();
        while (servers.MoveNext(out var id, out var server))
        {
            if (!this.IsPowered(id, EntityManager))
                continue;

            PruneStaleSensors(server, now);

            if (TryComp<DeviceNetworkComponent>(id, out var device))
                BroadcastSensorStatus(id, server, device);
        }
    }

    /// <summary>
    /// Adds or updates a sensor status entry if the received package is a sensor status update
    /// </summary>
    private void OnPacketReceived(EntityUid uid, CrewMonitoringServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!this.IsPowered(uid, EntityManager))
            return;

        var sensorStatus = _sensors.PacketToSuitSensor(args.Data);
        if (sensorStatus == null)
            return;

        var serverTransform = Transform(uid);

        // Sunrise - Edit: сервер принимает только датчики в своём радиусе действия.
        var owner = GetEntity(sensorStatus.OwnerUid);
        if (!EntityManager.EntityExists(owner))
            return;

        var serverPos = _transformSystem.GetWorldPosition(serverTransform);
        var ownerPos = _transformSystem.GetWorldPosition(Transform(owner));
        var rangeSquared = component.MonitoringRange * component.MonitoringRange;
        if ((serverPos - ownerPos).LengthSquared() > rangeSquared)
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
    private static void PruneStaleSensors(CrewMonitoringServerComponent component, TimeSpan now)
    {
        var timeout = TimeSpan.FromSeconds(component.SensorTimeout);
        if (component.SensorStatus.Count == 0)
            return;

        List<string>? toRemove = null;

        foreach (var (address, sensor) in component.SensorStatus)
        {
            if (now - sensor.Timestamp <= timeout)
                continue;

            toRemove ??= new List<string>();
            toRemove.Add(address);
        }

        if (toRemove == null)
            return;

        foreach (var address in toRemove)
            component.SensorStatus.Remove(address);
    }

    /// <summary>
    /// Broadcasts the status of all connected sensors
    /// </summary>
    private void BroadcastSensorStatus(EntityUid uid, CrewMonitoringServerComponent serverComponent, DeviceNetworkComponent device)
    {
        var serverGrid = Transform(uid).GridUid;

        // Фильтруем датчики в радиусе сервера.
        var filteredStatus = new Dictionary<string, SuitSensorStatus>(serverComponent.SensorStatus);

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdUpdatedState,
            [SuitSensorConstants.NET_STATUS_COLLECTION] = filteredStatus, // Sunrise - Added
        };
        if (serverGrid != null)
            payload[CrewMonitoringNetKeys.MonitoringGrid] = GetNetEntity(serverGrid.Value);

        _deviceNetworkSystem.QueuePacket(uid, null, payload, device: device);
    }

}
