namespace Content.Shared._Sunrise.Weapons.DualWield;

/// <summary>
/// Маркер для оружия, разрешающий использование в режиме "стрельбы по македонски".
/// Добавляйте это на пистолеты и ПП. Без этого компонента режим будет недоступен.
/// </summary>
[RegisterComponent]
public sealed partial class CanDualWieldComponent : Component
{
    /// <summary>
    /// Дополнительный разброс (в градусах) при активном режиме двойной стрельбы.
    /// Добавляется к MinAngle и MaxAngle обоих пистолетов.
    /// Используйте высокие значения (20-45°) для маломощного оружия.
    /// </summary>
    [DataField]
    public float DualWieldInaccuracyPenalty = 15f;
}
