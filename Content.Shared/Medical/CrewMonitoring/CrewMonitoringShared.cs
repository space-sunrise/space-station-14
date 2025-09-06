using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.CrewMonitoring;

[Serializable, NetSerializable]
public enum CrewMonitoringUIKey
{
    Key
}
//Sunrise-Start
[Serializable, NetSerializable]
public enum CrewMonitoringMode : byte
{
    /// <summary>
    /// Monitoring alarm is off
    /// </summary>
    ToggleOff = 0,

    /// <summary>
    /// Monitoring alarm is triggered when the mob's status changes with working sensors
    /// </summary>
    StateChanged = 1,

    /// <summary>
    /// Monitoring alarm is triggered when anyone is dead with working sensors
    /// </summary>
    AnyoneDead = 2,

    /// <summary>
    /// Both mode
    /// </summary>
    Both = 3
}
//Sunrise-End
[Serializable, NetSerializable]
public sealed class CrewMonitoringState : BoundUserInterfaceState
{
    public List<SuitSensorStatus> Sensors;

    public CrewMonitoringState(List<SuitSensorStatus> sensors)
    {
        Sensors = sensors;
    }
}
