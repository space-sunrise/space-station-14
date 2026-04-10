using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Inventory.Components;

/// <summary>
/// Отслеживает приоритеты личного хранилища для игрока.
/// Сопоставляет объект хранения с приоритетным объектом элемента.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PersonalStoragePriorityComponent : Component
{
    /// <summary>
    /// Словарь объекта хранения для объекта приоритетного элемента.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, EntityUid> Priorities = new();
}