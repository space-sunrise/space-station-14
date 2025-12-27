using Content.Shared.Medical.CrewMonitoring;
using Robust.Client.UserInterface;
using Robust.Shared.Localization;
using Robust.Shared.Map;

namespace Content.Client._Sunrise.Medical.CrewMonitoring;

/// <summary>
/// Вся фильтрация данных должна происходить на сервере, клиент только отображает полученное состояние.
/// </summary>
public class SunriseCrewMonitoringBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    protected SunriseCrewMonitoringWindow? Menu;

    protected virtual string? TitleLocKey => null;

    public SunriseCrewMonitoringBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        Menu = this.CreateWindow<SunriseCrewMonitoringWindow>();
        Menu.Set(string.Empty, null);
        Menu.SetBoundUserInterface(this);

        var titleLocKey = TitleLocKey;

        if (titleLocKey != null)
            Menu.Title = Loc.GetString(titleLocKey);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CrewMonitoringState st)
            return;

        var menu = Menu;
        if (menu == null)
            return;

        EntityUid? monitoringGridUid = null;
        var stationName = string.Empty;

        if (st.MonitoringGrid.HasValue)
        {
            monitoringGridUid = EntMan.GetEntity(st.MonitoringGrid.Value);

            if (EntMan.TryGetComponent<MetaDataComponent>(monitoringGridUid, out var metaData))
                stationName = metaData.EntityName;
        }

        menu.Set(stationName, monitoringGridUid);

        EntityCoordinates? monitorCoords = null;

        if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
            monitorCoords = xform.Coordinates;

        menu.ShowSensors(st.Sensors, Owner, monitorCoords, st.HasServer, st.NoSensorsReason);
        menu.UpdateCorpseAlertToggle(st.CorpseAlertEnabled);
    }
}
