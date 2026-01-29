using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CarpQueen;

/// <summary>
/// Компонент хранит память о жидкости, из которой вылупился карп,
/// включая цвет и реагенты для впрыска при укусе.
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
    /// Список всех цветов из жидкостей, где вылупился карп.
    /// Используется радужными карпами для цикличной смены цветов.
    /// </summary>
    [DataField("liquidColors"), AutoNetworkedField]
    public List<Color> LiquidColors = new();

    /// <summary>
    /// Словарь ID реагентов и их количеств из жидкости.
    /// Используется для впрыска при укусе.
    /// </summary>
    [DataField("rememberedReagents"), AutoNetworkedField]
    public Dictionary<string, FixedPoint2> RememberedReagents = new();

    /// <summary>
    /// Количество каждого запомненного реагента для впрыска при укусе.
    /// </summary>
    [DataField("biteReagentAmount")]
    public FixedPoint2 BiteReagentAmount = FixedPoint2.New(1);

    /// <summary>
    /// Список игроков, которые были рядом при вылуплении карпа.
    /// Эти игроки считаются "друзьями" и не атакуются, пока королева не прикажет.
    /// </summary>
    [DataField("rememberedFriends"), AutoNetworkedField]
    public HashSet<EntityUid> RememberedFriends = new();

    /// <summary>
    /// Список сущностей, которых карп временно запрещено атаковать.
    /// Очищается когда атакующий наносит урон владельцу карпа.
    /// </summary>
    [DataField("forbiddenTargets")]
    public HashSet<EntityUid> ForbiddenTargets = new();
}

