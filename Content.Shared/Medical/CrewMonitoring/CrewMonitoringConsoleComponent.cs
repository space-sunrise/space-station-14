using Content.Shared.Audio;//Sunrise-Edit
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.CrewMonitoring;//Sunrise-Edit
using Robust.Shared.Audio;
using Content.Shared._Sunrise.Medical;//Sunrise-Edit
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent]
[Access(typeof(SharedCrewMonitoringConsoleSystem))]//Sunrise-Edit
public sealed partial class CrewMonitoringConsoleComponent : Component
{
    /// <summary>
    ///     List of all currently connected sensors to this console.
    /// </summary>
    public Dictionary<string, SuitSensorStatus> ConnectedSensors = new();

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField("sensorTimeout"), ViewVariables(VVAccess.ReadWrite)]
    public float SensorTimeout = 10f;

    /// <summary>
    ///     Whether the console should beep when corpses with sensors are detected outside morgues.
    /// </summary>
    [DataField("CrewMonitoringMode"), ViewVariables(VVAccess.ReadWrite)]
    public CrewMonitoringMode Mode = CrewMonitoringMode.ToggleOff;

    /// <summary>
    ///     Next time to check for corpses and potentially play alert sound.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextCorpseAlertTime = TimeSpan.Zero;

    /// <summary>
    ///     The amount of time between each corpse alert beep.
    /// </summary>
    [DataField("corpseAlertTime"), ViewVariables(VVAccess.ReadWrite)]
    public float CorpseAlertTime = 15f;

    /// <summary>
    ///     Sound to play when corpses with sensors are detected outside morgues.
    /// </summary>
    [DataField("corpseAlertSound")]
    public SoundSpecifier CorpseAlertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg", AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation).WithVolume(-3f));
}
