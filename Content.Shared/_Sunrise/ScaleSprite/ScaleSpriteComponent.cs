using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.ScaleSprite;

/// <summary>
/// Компонент меняет только визуальную часть объекта. Необходим для трейтов высокий и низкий.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ScaleSpriteComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2 Scale = Vector2.One;
}
