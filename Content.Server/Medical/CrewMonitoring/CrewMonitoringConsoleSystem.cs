using Content.Server.PowerCell;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Morgue.Components;
using Content.Shared.Pinpointer;
using Content.Server.Power.EntitySystems; // Sunrise - Edit
using Content.Shared.Power.Components; // Sunrise - Edit
using Content.Shared.UserInterface; // Sunrise - Edit
using Content.Shared.Storage.Components;
using Content.Shared.Verbs; // Sunrise - Edit
using Robust.Server.GameObjects;
using Content.Shared.Implants.Components; // Sunrise - Edit
using Content.Server._Sunrise.Medical.CrewMonitoring; // Sunrise - Edit
using Content.Shared.Mobs.Systems; // Sunrise - Edit
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;


namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    // Sunrise - Start
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private EntityQuery<CrewMonitoringFilterComponent> _filterQuery;
    // Sunrise - End

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringToggleCorpseAlertMessage>(OnToggleCorpseAlert);//Sunrise-Edit
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, GetVerbsEvent<InteractionVerb>>(AddToggleVerb);//Sunrise-Edit

        _filterQuery = GetEntityQuery<CrewMonitoringFilterComponent>(); // Sunrise - Edit
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CrewMonitoringConsoleComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            var uiOpen = _uiSystem.IsUiOpen(uid, CrewMonitoringUIKey.Key);
            if (!uiOpen && !component.DoCorpseAlert)
                continue;

            var uiDirty = MaintainCache(component);
            if (uiDirty && uiOpen)
                UpdateUserInterface(uid, component, cacheUpToDate: true);

            if (!component.DoCorpseAlert)
                continue;

            if (component.NextCorpseAlertTime > _gameTiming.CurTime)
                continue;

            component.NextCorpseAlertTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.CorpseAlertTime);

            // Check for corpses with sensors outside morgues
            if (HasCorpsesOutsideMorgue(uid, component))
            {
                if (CanPlayCorpseAlert(uid))
                    _audio.PlayPvs(component.CorpseAlertSound, uid);
            }
        }
    }

    private bool CanPlayCorpseAlert(EntityUid uid)
    {
        if (HasComp<ActivatableUIRequiresPowerCellComponent>(uid) && TryComp<PowerCellDrawComponent>(uid, out var draw))
            return _cell.HasActivatableCharge(uid, draw);

        if (HasComp<ActivatableUIRequiresPowerComponent>(uid))
            return this.IsPowered(uid, EntityManager);

        return false;
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

        // Sunrise - Start
        payload.TryGetValue(CrewMonitoringNetKeys.MonitoringGrid, out NetEntity? monitoringGrid);
        component.MonitoringGrid = monitoringGrid.HasValue ? GetEntity(monitoringGrid.Value) : null;
        component.LastServerStateReceived = _gameTiming.CurTime;

        // Серверов может быть несколько, поэтому список датчиков поддерживаем как объединение
        // Протухшие записи будут чиститься по Timestamp + SensorTimeout
        foreach (var (address, status) in sensorStatus)
            component.ConnectedSensors[address] = status;
        // Sunrise - End
        UpdateUserInterface(uid, component);
    }

    private void OnUIOpened(EntityUid uid, CrewMonitoringConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!_cell.TryUseActivatableCharge(uid))
            return;

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(
        EntityUid uid,
        CrewMonitoringConsoleComponent? component = null,
        bool cacheUpToDate = false)
    {
        if (!Resolve(uid, ref component))
            return;

        // Sunrise - Start
        if (!cacheUpToDate)
            MaintainCache(component);

        var hasServer = HasServer(component);

        // Фильтруем список на сервере, чтобы не отдавать лишние датчики клиенту (анти-чит)
        var sensorsResult = hasServer
            ? GetVisibleSensorsInternal(uid, component)
            : new VisibleSensorsResult(new List<SuitSensorStatus>(), CrewMonitoringNoSensorsReason.None);

        NetEntity? monitoringGridNet = null;
        if (hasServer && component.MonitoringGrid is { } monitoringGrid && EntityManager.EntityExists(monitoringGrid))
        {
            EnsureComp<NavMapComponent>(monitoringGrid);
            monitoringGridNet = GetNetEntity(monitoringGrid);
        }

        _uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, new CrewMonitoringState(
            sensorsResult.Sensors,
            component.DoCorpseAlert,
            monitoringGridNet,
            hasServer,
            sensorsResult.NoSensorsReason));
        // Sunrise - End
    }

    // Sunrise - Start
    private List<SuitSensorStatus> GetVisibleSensors(EntityUid uid, CrewMonitoringConsoleComponent component)
    {
        return GetVisibleSensorsInternal(uid, component).Sensors;
    }

    private readonly record struct VisibleSensorsResult(List<SuitSensorStatus> Sensors, CrewMonitoringNoSensorsReason NoSensorsReason);

    private VisibleSensorsResult GetVisibleSensorsInternal(EntityUid uid, CrewMonitoringConsoleComponent component)
    {
        if (!_filterQuery.TryComp(uid, out var filter))
        {
            var unfilteredSensors = new List<SuitSensorStatus>(component.ConnectedSensors.Values);
            return new VisibleSensorsResult(
                unfilteredSensors,
                unfilteredSensors.Count == 0 ? CrewMonitoringNoSensorsReason.NoSensors : CrewMonitoringNoSensorsReason.None);
        }

        HashSet<string>? allowedSet = null;
        if (filter.AllowedDepartmentIds.Count > 0)
            allowedSet = new HashSet<string>(filter.AllowedDepartmentIds);

        var sensors = new List<SuitSensorStatus>();
        var matchedBySource = 0;
        foreach (var sensor in component.ConnectedSensors.Values)
        {
            if (!IsSensorAllowedBySource(sensor, allowedSet, filter))
                continue;

            matchedBySource++;

            // Доп. ограничение: показывать только крит/трупы.
            if (filter.OnlyShowWoundedOrDead && !IsCriticalOrDead(sensor))
                continue;

            sensors.Add(sensor);
        }

        if (sensors.Count > 0)
            return new VisibleSensorsResult(sensors, CrewMonitoringNoSensorsReason.None);

        if (component.ConnectedSensors.Count == 0)
            return new VisibleSensorsResult(sensors, CrewMonitoringNoSensorsReason.NoSensors);

        if (filter.OnlyShowWoundedOrDead && matchedBySource > 0)
            return new VisibleSensorsResult(sensors, CrewMonitoringNoSensorsReason.NoWoundedOrDead);

        return new VisibleSensorsResult(sensors, CrewMonitoringNoSensorsReason.NoMatchingSensors);
    }

    private bool IsSensorAllowedBySource(SuitSensorStatus sensor, HashSet<string>? allowedSet, CrewMonitoringFilterComponent filter)
    {
        if (allowedSet == null && !filter.IncludeTrackers)
            return true;

        if (allowedSet != null)
        {
            foreach (var departmentId in sensor.JobDepartmentIds)
            {
                if (allowedSet.Contains(departmentId))
                    return true;
            }
        }

        return filter.IncludeTrackers && IsTrackerSensor(sensor);
    }

    private bool IsTrackerSensor(SuitSensorStatus sensor)
    {
        var sensorUid = GetEntity(sensor.SuitSensorUid);
        return EntityManager.EntityExists(sensorUid) && HasComp<SubdermalImplantComponent>(sensorUid);
    }


    private bool IsCriticalOrDead(SuitSensorStatus sensor)
    {
        // Для мёртвых нам хватает бинарного статуса.
        if (!sensor.IsAlive)
            return true;

        var owner = GetEntity(sensor.OwnerUid);
        if (EntityManager.EntityExists(owner) && _mobState.IsIncapacitated(owner))
            return true;

        return false;
    }

    private bool MaintainCache(CrewMonitoringConsoleComponent component)
    {
        var now = _gameTiming.CurTime;
        var timeout = TimeSpan.FromSeconds(component.SensorTimeout);

        var dirty = PruneStaleSensors(component, now, timeout);

        if (component.LastServerStateReceived == TimeSpan.Zero)
            return dirty;

        if (now - component.LastServerStateReceived <= timeout)
            return dirty;

        component.LastServerStateReceived = TimeSpan.Zero;

        if (component.MonitoringGrid != null || component.ConnectedSensors.Count > 0)
        {
            component.MonitoringGrid = null;
            component.ConnectedSensors.Clear();
        }

        return true;
    }

    private bool HasServer(CrewMonitoringConsoleComponent component)
    {
        if (component.LastServerStateReceived == TimeSpan.Zero)
            return false;

        var timeout = TimeSpan.FromSeconds(component.SensorTimeout);
        return _gameTiming.CurTime - component.LastServerStateReceived < timeout;
    }

    private static bool PruneStaleSensors(
        CrewMonitoringConsoleComponent component,
        TimeSpan now,
        TimeSpan timeout)
    {
        if (component.ConnectedSensors.Count == 0)
            return false;

        List<string>? toRemove = null;
        foreach (var (address, sensor) in component.ConnectedSensors)
        {
            if (now - sensor.Timestamp <= timeout)
                continue;

            toRemove ??= new List<string>();
            toRemove.Add(address);
        }

        if (toRemove == null)
            return false;

        foreach (var address in toRemove)
            component.ConnectedSensors.Remove(address);

        return true;
    }
    // Sunrise - End


    /// <summary>
    /// Checks if there are any corpses with active sensors outside of morgues
    /// </summary>
    private bool HasCorpsesOutsideMorgue(EntityUid uid, CrewMonitoringConsoleComponent component)
    {
        foreach (var sensor in GetVisibleSensors(uid, component))
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
            foreach (var contained in storage.Contents.ContainedEntities)
            {
                if (contained == entity)
                    return true;
            }
        }

        return false;
    }
    // Sunrise - Start
    private void OnToggleCorpseAlert(EntityUid uid, CrewMonitoringConsoleComponent component, CrewMonitoringToggleCorpseAlertMessage args)
    {
        component.DoCorpseAlert = !component.DoCorpseAlert;
        UpdateUserInterface(uid, component);
    }
    private void AddToggleVerb(EntityUid uid, CrewMonitoringConsoleComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        InteractionVerb verb = new();
        if (component.DoCorpseAlert)
            verb.Text = _loc.GetString("item-toggle-deactivate-alert");
        else
            verb.Text = _loc.GetString("item-toggle-activate-alert");

        verb.Act = () => ToggleAlert(uid, component);
        args.Verbs.Add(verb);
    }

    public void ToggleAlert(EntityUid uid, CrewMonitoringConsoleComponent component)
    {
        component.DoCorpseAlert = !component.DoCorpseAlert;
        Dirty(uid, component);
        UpdateUserInterface(uid, component);
    }
    //Sunrise-End
}
