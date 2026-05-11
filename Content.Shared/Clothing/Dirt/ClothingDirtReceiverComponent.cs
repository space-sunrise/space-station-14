using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Dirt;

/// <summary>
/// Добавляется на гуманоида. Хранит цвет крови его расы.
/// Благодаря этому компоненту система знает, какой цвет использовать для кровяных пятен.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ClothingDirtReceiverComponent : Component
{
    /// <summary>
    /// Цвет крови расы персонажа. Устанавливается при инициализации на основе HumanoidAppearanceComponent.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color BloodColor = Color.FromHex("#AA0000");
}
