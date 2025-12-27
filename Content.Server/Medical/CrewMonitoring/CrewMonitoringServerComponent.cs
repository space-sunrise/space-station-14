using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Map;

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent]
[Access(typeof(CrewMonitoringServerSystem))]
public sealed partial class CrewMonitoringServerComponent : Component
{

    /// <summary>
    ///     List of all currently connected sensors to this server.
    /// </summary>
    public readonly Dictionary<string, SuitSensorStatus> SensorStatus = new();
    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField]
    public float SensorTimeout = 10f;


    // Sunrise - Start
    /// <summary>
    ///     Максимальная дистанция в тайлах, на которой сервер считает датчики доступными
    /// </summary>
    [DataField]
    public float MonitoringRange = 75f;
    // Sunrise - End
}
