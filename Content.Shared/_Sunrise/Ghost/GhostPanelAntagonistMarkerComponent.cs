using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Ghost;

/// <summary>
/// Компонент, отвечающий за отобрание сущности в панели призрака как антагониста
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GhostPanelAntagonistMarkerComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public LocId Name;

    [DataField(required: true), AutoNetworkedField]
    public LocId Description;

    /// <summary>
    /// Приоритет отображения антагониста. Влияет на расположение в панели призрака для сортировки по значимости
    /// Чем ниже число, тем выше приоритет.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public int Priority;
}
