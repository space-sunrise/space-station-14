using Content.Shared.Medical.CrewMonitoring;

namespace Content.Client.Medical.CrewMonitoring;

public sealed class CrewMonitoringBoundUserInterface : SunriseCrewMonitoringBoundUserInterface
{
    public CrewMonitoringBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }
}
