using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Dirt;

/// <summary>
/// Компонент загрязнения одежды.
/// Добавляется на предметы одежды, которые могут быть испачканы.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ClothingDirtComponent : Component
{
    /// <summary>
    /// Текущий уровень загрязнения (0.0 = чистое, 1.0 = максимально грязное).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DirtLevel = 0f;

    /// <summary>
    /// Цвет пятна (коричневый для грязи, цвет крови расы для ранений).
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color DirtColor = Color.FromHex("#5C3D1E");

    /// <summary>
    /// Сколько загрязнения добавляется за одно событие.
    /// </summary>
    [DataField]
    public float DirtPerEvent = 0.25f;

    /// <summary>
    /// Скорость очистки водой (за единицу воды).
    /// </summary>
    [DataField]
    public float CleanRate = 0.15f;

    /// <summary>
    /// Может ли этот предмет получать кровяные пятна (куртки, комбинезоны).
    /// </summary>
    [DataField]
    public bool CanGetBloody = false;
}
