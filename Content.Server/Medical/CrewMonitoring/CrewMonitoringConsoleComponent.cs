using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent]
[Access(typeof(CrewMonitoringConsoleSystem))]
public sealed partial class CrewMonitoringConsoleComponent : Component
{
    /// <summary>
    ///     List of all currently connected sensors to this console.
    /// </summary>
    public Dictionary<string, SuitSensorStatus> ConnectedSensors = new();

    // Sunrise - Start
    /// <summary>
    ///     Грид который отображается на карте (только грид где у становлен сервер мониторинга)
    /// </summary>
    public EntityUid? MonitoringGrid;

    /// <summary>
    ///     Время последнего полученного состояния от сервера. Нужно чтобы отличать состояния "сервер не найден"
    ///     от "сервер есть, но датчиков нет"
    /// </summary>
    public TimeSpan LastServerStateReceived;

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField]
    public float SensorTimeout = 10f;

    /// <summary>
    ///     Whether the console should beep when corpses with sensors are detected outside morgues.
    /// </summary>
    [DataField]
    public bool DoCorpseAlert = false;

    /// <summary>
    ///     Next time to check for corpses and potentially play alert sound.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextCorpseAlertTime = TimeSpan.Zero;

    /// <summary>
    ///     The amount of time between each corpse alert beep.
    /// </summary>
    [DataField]
    public float CorpseAlertTime = 15f;

    /// <summary>
    ///     Sound to play when corpses with sensors are detected outside morgues.
    /// </summary>
    [DataField]
    public SoundSpecifier CorpseAlertSound =
        new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");
    // Sunrise - End
}
