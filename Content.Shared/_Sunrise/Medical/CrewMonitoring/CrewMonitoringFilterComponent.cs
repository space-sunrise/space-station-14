using Robust.Shared.GameStates;

namespace Content.Shared.Medical.CrewMonitoring;

[RegisterComponent]
public sealed partial class CrewMonitoringFilterComponent : Component
{
    /// <summary>
    /// Разрешенные отделы. Если пусто – все доступны.
    /// </summary>
    [DataField("allowedDepartmentIds")]
    public List<string> AllowedDepartmentIds = new();

    /// </summary>
    /// Будут ли отображаться трекеры (импланты).
    /// </summary>
    [DataField("includeTrackers")]
    public bool IncludeTrackers;

    /// </summary>
    /// Показывать здоровых (урон 0% – 13.2%)
    /// </summary>
    [DataField("showHealthy")]
    public bool ShowHealthy;
    /// </summary>
    ///Показывать состояние "хорошо" (урон 13.2% – 36%)
    /// </summary>
    [DataField("showGood")]
    public bool ShowGood;
    /// </summary>
    ///Показывать состояние "не очень" (урон 36% – 60%)
    /// </summary>
    [DataField("showNotGreat")]
    public bool ShowNotGreat;
    /// </summary>
    ///Показывать состояние "плохо" (урон 60% – 83%)
    /// </summary>
    [DataField("showBad")]
    public bool ShowBad;
    /// </summary>
    ///Показывать состояние "ужасно" (урон 83% – 100%)
    /// </summary>
    [DataField("showTerrible")]
    public bool ShowTerrible;
    /// </summary>
    ///Показывать критическое состояние (урон >= 100%)
    /// </summary>
    [DataField("showCritical")]
    public bool ShowCritical;
    /// </summary>
    ///Показывать мёртвых
    /// </summary>
    [DataField("showDead")]
    public bool ShowDead;
}

