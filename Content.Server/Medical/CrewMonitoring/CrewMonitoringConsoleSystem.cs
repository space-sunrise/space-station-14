using System.Linq;
using Content.Server.PowerCell;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Morgue.Components;
using Content.Shared.Pinpointer;
using Content.Server.Power.EntitySystems;//Sunrise-Edit
using Content.Shared.Power.Components;//Sunrise-Edit
using Content.Shared.PowerCell;//Sunrise-Edit
using Content.Shared.UserInterface;//Sunrise-Edit
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;//Sunrise-Edit
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

//Sunrise-Edit

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
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringToggleCorpseAlertMessage>(OnToggleCorpseAlert);//Sunrise-Edit
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, GetVerbsEvent<InteractionVerb>>(AddToggleVerb);//Sunrise-Edit
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
                if (HasComp<ActivatableUIRequiresPowerCellComponent>(uid) && TryComp<PowerCellDrawComponent>(uid, out var draw))
                {
                    if (_cell.HasActivatableCharge(uid, draw))
                    {
                        _audio.PlayPvs(component.CorpseAlertSound, uid);
                    }
                }
                if (HasComp<ActivatableUIRequiresPowerComponent>(uid))
                {
                    if (this.IsPowered(uid, EntityManager))
                    {
                        _audio.PlayPvs(component.CorpseAlertSound, uid);
                    }
                }
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
        _uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, new CrewMonitoringState(allSensors, component.DoCorpseAlert));
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

    //Sunrise-Start
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
        {
            verb.Text = Loc.GetString("item-toggle-deactivate-alert");
        }
        else
        {
            verb.Text = Loc.GetString("item-toggle-activate-alert");
        }
        verb.Act = () => ToggleAlert(uid, component);
        args.Verbs.Add(verb);
    }

    public void ToggleAlert(EntityUid uid, CrewMonitoringConsoleComponent component)
    {
        if (component.DoCorpseAlert)
        {
            component.DoCorpseAlert = false;
        }
        else
        {
            component.DoCorpseAlert = true;
        }
        Dirty(uid, component);
    }
    //Sunrise-End
}
