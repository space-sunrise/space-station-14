using System.Linq;
using Content.Shared.PowerCell;
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
using Content.Shared.PowerCell.Components;

//Sunrise-Edit

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!; // Sunrise - Added

    private const float CriticalDamagePercentage = 1.0f; // Sunrise - Added

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
                    if (_cell.HasActivatableCharge(uid))
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

    //Sunrise-Start: Alert
    /// <summary>
    /// Checks if there are any corpses with active sensors outside of morgues
    /// </summary>
    private bool HasCorpsesOutsideMorgue(CrewMonitoringConsoleComponent component)
    {
        foreach (var sensor in component.ConnectedSensors.Values)
        {
            var damagePercentage = sensor.DamagePercentage;
            var isCritical = damagePercentage.HasValue && damagePercentage.Value >= CriticalDamagePercentage;

            if (sensor.IsAlive && !isCritical)
                continue;

            if (!TryGetEntity(sensor.OwnerUid, out var ownerUid))
                continue;

            if (!IsEntityInMorgue(ownerUid.Value))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the given entity is inside a morgue entity storage
    /// </summary>
    private bool IsEntityInMorgue(EntityUid entity)
    {
        var parent = Transform(entity).ParentUid;

        return parent.IsValid() && HasComp<MorgueComponent>(parent);
    }

    private void OnToggleCorpseAlert(Entity<CrewMonitoringConsoleComponent> ent, ref CrewMonitoringToggleCorpseAlertMessage args)
    {
        var (uid, component) = ent;
        component.DoCorpseAlert = !component.DoCorpseAlert;
        UpdateUserInterface(uid, component);
    }

    private void AddToggleVerb(Entity<CrewMonitoringConsoleComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        var (uid, component) = ent;

        if (!args.CanInteract || !args.CanAccess)
            return;

        InteractionVerb verb = new();

        verb.Text = _loc.GetString(component.DoCorpseAlert
            ? "item-toggle-deactivate-alert"
            : "item-toggle-activate-alert");

        verb.Act = () => ToggleAlert(ent);
        args.Verbs.Add(verb);
    }

    public void ToggleAlert(Entity<CrewMonitoringConsoleComponent> ent)
    {
        var (uid, component) = ent;
        component.DoCorpseAlert = !component.DoCorpseAlert;

        Dirty(uid, component);
        UpdateUserInterface(uid, component);
    }
    //Sunrise-End: Alert
}
