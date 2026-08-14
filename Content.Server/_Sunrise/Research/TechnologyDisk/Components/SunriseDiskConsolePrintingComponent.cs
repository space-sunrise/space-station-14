using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Research.TechnologyDisk.Components;

[RegisterComponent]
public sealed partial class SunriseDiskConsolePrintingComponent : Component
{
    /// <summary>
    /// Момент завершения текущей печати.
    /// </summary>
    public TimeSpan FinishTime;

    /// <summary>
    /// Прототип диска, выбранный при запуске печати.
    /// </summary>
    public EntProtoId DiskPrototype;
}
