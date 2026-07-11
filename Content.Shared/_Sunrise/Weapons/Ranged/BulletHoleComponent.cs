using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Weapons.Ranged;

/// <summary>
/// Хранит серверное состояние следов от пуль на поверхности.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BulletHoleSystem))]
public sealed partial class BulletHoleComponent : Component
{
    /// <summary>
    /// Количество подтверждённых попаданий.
    /// </summary>
    public int Count;

    /// <summary>
    /// Номер выбранной конфигурации следов.
    /// </summary>
    public int State;
}

/// <summary>
/// Помечает снаряд, который оставляет следы при нанесении урона стене.
/// </summary>
[RegisterComponent]
public sealed partial class BulletHoleGeneratorComponent : Component;

[Serializable, NetSerializable]
public enum BulletHoleVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum BulletHoleVisualLayers : byte
{
    BulletHole,
}
