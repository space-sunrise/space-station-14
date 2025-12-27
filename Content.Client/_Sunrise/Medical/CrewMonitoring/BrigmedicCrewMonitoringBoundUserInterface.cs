using Content.Shared.Medical.CrewMonitoring;

namespace Content.Client.Medical.CrewMonitoring.Brigmedic;

public sealed class BrigmedicCrewMonitoringBoundUserInterface : SunriseCrewMonitoringBoundUserInterface
{
    protected override string? TitleLocKey => "crew-monitoring-ui-title-brigmedic";

    public BrigmedicCrewMonitoringBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }
}
