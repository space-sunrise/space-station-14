using Content.Shared.Medical.CrewMonitoring;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using System.Linq;

namespace Content.Client.Medical.CrewMonitoring.Brigmedic;

public sealed class BrigmedicCrewMonitoringBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CrewMonitoringWindow? _menu;

    public BrigmedicCrewMonitoringBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<CrewMonitoringWindow>();
        _menu.Set(string.Empty, null);
        _menu.SetBoundUserInterface(this);
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

                var securityDepartmentSensors = st.Sensors
                    .Where(sensor => sensor.JobDepartmentIds.Contains("Security"))
                    .ToList();

                EntityCoordinates? monitorCoords = null;
                if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
                    monitorCoords = xform.Coordinates;

                _menu?.ShowSensors(securityDepartmentSensors, Owner, monitorCoords, st.HasServer);
                _menu?.UpdateCorpseAlertToggle(st.CorpseAlertEnabled);
                break;
        }
    }
}
