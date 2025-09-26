using System.Linq;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.PowerCell;
using Content.Server.Station.Systems;
using Content.Server.Storage.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Morgue.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Storage.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CrewMonitoringConsoleComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.DoCorpseAlert)
                continue;

            if (component.NextCorpseAlertTime > _gameTiming.CurTime)
                continue;

            component.NextCorpseAlertTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.CorpseAlertTime);

            // Check for corpses with sensors outside morgues
            if (HasCorpsesOutsideMorgue(component))
            {
                _audio.PlayPvs(component.CorpseAlertSound, uid);
            }
        }
    }

    private void OnRemove(EntityUid uid, CrewMonitoringConsoleComponent component, ComponentRemove args)
    {
        component.ConnectedSensors.Clear();
    }

    private void OnPacketReceived(EntityUid uid, CrewMonitoringConsoleComponent component, DeviceNetworkPacketEvent args)
    {
        var payload = args.Data;

        // Check command
        if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        if (command != DeviceNetworkConstants.CmdUpdatedState)
            return;

        if (!payload.TryGetValue(SuitSensorConstants.NET_STATUS_COLLECTION, out Dictionary<string, SuitSensorStatus>? sensorStatus))
            return;

        if (!payload.TryGetValue(SuitSensorConstants.MAP_ID, out MapId mapId))
            return;

        var consoleTransform = Transform(uid);
        var consoleMapId = consoleTransform.MapID;

        if (mapId != consoleMapId)
            return;

        component.ConnectedSensors = sensorStatus;
        UpdateUserInterface(uid, component);
    }

    private void OnUIOpened(EntityUid uid, CrewMonitoringConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!_cell.TryUseActivatableCharge(uid))
            return;

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, CrewMonitoringConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_uiSystem.IsUiOpen(uid, CrewMonitoringUIKey.Key))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        var xform = Transform(uid);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        // Update all sensors info
        var allSensors = component.ConnectedSensors.Values.ToList();
        _uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, new CrewMonitoringState(allSensors));
    }

    /// <summary>
    /// Checks if there are any corpses with active sensors outside of morgues
    /// </summary>
    private bool HasCorpsesOutsideMorgue(CrewMonitoringConsoleComponent component)
    {
        foreach (var sensor in component.ConnectedSensors.Values)
        {
            // Skip if the person is alive
            if (sensor.IsAlive)
                continue;

            // Check if the sensor owner entity is inside a morgue
            var ownerUid = GetEntity(sensor.OwnerUid);
            if (!EntityManager.EntityExists(ownerUid))
                continue;

            // Check if the corpse is inside a morgue
            if (!IsEntityInMorgue(ownerUid))
                return true; // Found a corpse outside morgue
        }

        return false;
    }

    /// <summary>
    /// Checks if the given entity is inside a morgue entity storage
    /// </summary>
    private bool IsEntityInMorgue(EntityUid entity)
    {
        // Check if the entity is contained within any morgue
        var morgueQuery = EntityQueryEnumerator<MorgueComponent, EntityStorageComponent>();

        while (morgueQuery.MoveNext(out var morgueUid, out var morgue, out var storage))
        {
            // Check if the entity is contained in this morgue
            if (storage.Contents.ContainedEntities.Contains(entity))
            {
                return true;
            }
        }

        return false;
    }
}
