using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Weapons.DualWield;

/// <summary>
/// Добавляется на персонажа при активации режима "стрельбы по македонски".
/// Отслеживает активность режима и чередование выстрелов (левый/правый пистолет).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DualWieldComponent : Component
{
    /// <summary>
    /// Активен ли режим двойной стрельбы
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// Пистолет в левой руке
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid LeftGun;

    /// <summary>
    /// Пистолет в правой руке
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid RightGun;

    /// <summary>
    /// Если true, следующий выстрел из левого пистолета, иначе из правого.
    /// Чередуется после каждого выстрела.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NextIsLeft;
}
