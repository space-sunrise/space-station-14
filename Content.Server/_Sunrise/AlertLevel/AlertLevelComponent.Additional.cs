#pragma warning disable IDE0130 // Пространство имён соответствует расширяемому upstream-компоненту.
namespace Content.Server.AlertLevel;

public sealed partial class AlertLevelComponent
{
    /// <summary>
    /// Дополнительные коды, действующие одновременно с основным кодом станции.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public readonly HashSet<string> ActiveAdditionalLevels = [];
}
