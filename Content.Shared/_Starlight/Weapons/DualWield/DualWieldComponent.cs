using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Weapons.DualWield;

/// <summary>
/// Хранит состояние активной стрельбы по-македонски на владельце оружия.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DualWieldComponent : Component
{
    /// <summary>
    /// Активна ли стрельба по-македонски прямо сейчас.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// Оружие, которое считается первым при стрельбе.
    /// Обычно это оружие из активной руки в момент включения режима.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid LeftGun;

    /// <summary>
    /// Оружие из второй руки.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid RightGun;

    /// <summary>
    /// Если true — следующим выстрелит LeftGun.
    /// Если false — следующим выстрелит RightGun.
    /// После каждого успешного выстрела значение переключается.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NextIsLeft;
}
