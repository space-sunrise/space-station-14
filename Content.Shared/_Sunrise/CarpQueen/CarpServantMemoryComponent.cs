using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CarpQueen;

/// <summary>
/// Компонент хранит память о жидкости, из которой вылупился карп,
/// включая цвет и реагенты для инъекции при укусе.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CarpServantMemoryComponent : Component
{
    /// <summary>
    /// Цвет жидкости, из которой вылупился карп.
    /// Используется для визуального вида.
    /// </summary>
    [DataField("liquidColor"), AutoNetworkedField]
    public Color LiquidColor = Color.White;

    /// <summary>
    /// Словарь ID реагентов и их количества в жидкости.
    /// Используется для инъекции при укусе.
    /// </summary>
    [DataField("rememberedReagents"), AutoNetworkedField]
    public Dictionary<string, FixedPoint2> RememberedReagents = new();

    /// <summary>
    /// Количество каждого запомненного реагента для инъекции за один укус, в единицах.
    /// </summary>
    [DataField("biteReagentAmount")]
    public FixedPoint2 BiteReagentAmount = FixedPoint2.New(1);

    /// <summary>
    /// Список игроков, находившихся рядом при вылуплении карпа.
    /// Эти игроки считаются "друзьями" и не будут атакованы,
    /// если королева не прикажет иначе.
    /// </summary>
    [DataField("rememberedFriends"), AutoNetworkedField]
    public HashSet<EntityUid> RememberedFriends = new();

    /// <summary>
    /// Список сущностей, которых карпу временно запрещено атаковать.
    /// Очищается, когда атакующий наносит урон владельцу карпа.
    /// </summary>
    [DataField("forbiddenTargets")]
    public HashSet<EntityUid> ForbiddenTargets = new();
}
