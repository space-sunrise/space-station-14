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
public enum CrewMonitoringNoSensorsReason : byte
{
    None = 0,
    NoSensors = 1,
    NoMatchingSensors = 2,
    NoWoundedOrDead = 3,
}


[Serializable, NetSerializable]
public sealed class CrewMonitoringState : BoundUserInterfaceState
{
    public List<SuitSensorStatus> Sensors;
    public bool CorpseAlertEnabled;
    public NetEntity? MonitoringGrid;
    public bool HasServer;
    public CrewMonitoringNoSensorsReason NoSensorsReason;

    public CrewMonitoringState(
        List<SuitSensorStatus> sensors,
        bool corpseAlertEnabled = false,
        NetEntity? monitoringGrid = null,
        bool hasServer = false,
        CrewMonitoringNoSensorsReason noSensorsReason = CrewMonitoringNoSensorsReason.None) // Sunrise - edit
    {
        Sensors = sensors;
        CorpseAlertEnabled = corpseAlertEnabled;
        MonitoringGrid = monitoringGrid;
        HasServer = hasServer;
        NoSensorsReason = noSensorsReason;
    }
}
// Sunrise - End


[Serializable, NetSerializable]
public sealed class CrewMonitoringToggleCorpseAlertMessage : BoundUserInterfaceMessage
{
}
