using System.Numerics;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.Maths;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
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
    /// Выбранный вариант расположения следов от пуль.
    /// </summary>
    public int State;
}

/// <summary>
/// Позволяет снаряду оставлять следы от пуль на подходящих целях.
/// </summary>
[RegisterComponent]
public sealed partial class BulletHoleGeneratorComponent : Component
{
    /// <summary>
    /// Тип урона, который должен быть нанесён для создания следа от пули.
    /// </summary>
    [DataField]
    public ProtoId<DamageTypePrototype> RequiredDamageType = "Piercing";

    /// <summary>
    /// Цели, на которых этот снаряд может оставлять следы от пуль.
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetWhitelist;
}

/// <summary>
/// Данные для отображения следа от пули в локальном пространстве цели.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct BulletHoleVisualData(string State, Vector2 Offset, Angle Rotation);

[Serializable, NetSerializable]
public enum BulletHoleVisuals : byte
{
    Data,
}

[Serializable, NetSerializable]
public enum BulletHoleVisualLayers : byte
{
    BulletHole,
}
