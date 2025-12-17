using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.CrewMonitoring;

[Serializable, NetSerializable]
public enum CrewMonitoringUIKey
{
    Key
}

// Sunrise - Start
public static class CrewMonitoringNetKeys
{
    public const string MonitoringGrid = "crew-monitoring-grid";
}


[Serializable, NetSerializable]
public sealed class CrewMonitoringState : BoundUserInterfaceState
{
    public List<SuitSensorStatus> Sensors;
    public bool CorpseAlertEnabled;
    public NetEntity? MonitoringGrid;
    public bool HasServer;

    public CrewMonitoringState(List<SuitSensorStatus> sensors, bool corpseAlertEnabled = false, NetEntity? monitoringGrid = null, bool hasServer = false) // Sunrise - edit
    {
        Sensors = sensors;
        CorpseAlertEnabled = corpseAlertEnabled;
        MonitoringGrid = monitoringGrid;
        HasServer = hasServer;
    }
}
// Sunrise - End


[Serializable, NetSerializable]
public sealed class CrewMonitoringToggleCorpseAlertMessage : BoundUserInterfaceMessage
{
}
