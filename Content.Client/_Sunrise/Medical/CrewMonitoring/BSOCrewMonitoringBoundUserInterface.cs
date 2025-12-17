using Content.Shared.Medical.CrewMonitoring;
using Robust.Client.UserInterface;
using System.Linq;
using Content.Shared.Implants.Components;
using Robust.Shared.Map;
using Robust.Shared.Localization; // Sunrise - edit
namespace Content.Client.Medical.CrewMonitoring.BSO;

public sealed class BSOCrewMonitoringBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CrewMonitoringWindow? _menu;

    public BSOCrewMonitoringBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<CrewMonitoringWindow>();
        _menu.Set(string.Empty, null);
        _menu.SetBoundUserInterface(this);
        // Sunrise - Start
        _menu.Title = Loc.GetString("crew-monitoring-ui-title-command");
        // Sunrise - End
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        switch (state)
        {
            case CrewMonitoringState st:
                EntityUid? monitoringGridUid = null;
                var stationName = string.Empty;

                if (st.MonitoringGrid.HasValue)
                {
                    monitoringGridUid = EntMan.GetEntity(st.MonitoringGrid.Value);
                    if (EntMan.TryGetComponent<MetaDataComponent>(monitoringGridUid, out var metaData))
                        stationName = metaData.EntityName;
                }

                _menu?.Set(stationName, monitoringGridUid);

                var commandDepartmentSensors = st.Sensors
                    .Where(sensor => sensor.JobDepartmentIds.Contains("Command") || sensor.JobDepartmentIds.Contains("CentralCommand"))
                    .ToList();
                //also ALWAYS include the trackers
                //this is jank as there isnt a direct indication of a tracker in the suit sensor status
                //so we need to check the component directly
                foreach (var sensor in st.Sensors)
                {
                    //get the client entity
                    var clientEntity = EntMan.GetEntity(sensor.SuitSensorUid);
                    if (EntMan.TryGetComponent<SubdermalImplantComponent>(clientEntity, out _))
                    {
                        commandDepartmentSensors.Add(sensor);
                    }
                }
                //remove duplicates
                commandDepartmentSensors = commandDepartmentSensors.Distinct().ToList();

                EntityCoordinates? monitorCoords = null;
                if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
                    monitorCoords = xform.Coordinates;

                _menu?.ShowSensors(commandDepartmentSensors, Owner, monitorCoords, st.HasServer);
                _menu?.UpdateCorpseAlertToggle(st.CorpseAlertEnabled);
                break;
        }
    }
}
