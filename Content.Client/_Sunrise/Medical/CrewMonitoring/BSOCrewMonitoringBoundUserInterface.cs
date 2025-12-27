using Content.Client._Sunrise.Medical.CrewMonitoring;

namespace Content.Client._Sunrise.Medical.CrewMonitoring.BSO;

public sealed class BSOCrewMonitoringBoundUserInterface : SunriseCrewMonitoringBoundUserInterface
{
    protected override string? TitleLocKey => "crew-monitoring-ui-title-command";

    public BSOCrewMonitoringBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }
}
