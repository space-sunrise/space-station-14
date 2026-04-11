using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Weapons.DualWield;

/// <summary>
/// Разрешает оружию стрельбу по-македонски и хранит все его YAML-настройки.
/// Все значения ниже настраиваются только для режима стрельбы с двух рук.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CanDualWieldComponent : Component
{
    /// <summary>
    /// Включает или полностью выключает поддержку стрельбы по-македонски для конкретного оружия.
    /// true — оружие может использовать механику.
    /// false — оружие никогда не сможет стрелять по-македонски, даже если компонент унаследован от родителя.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Дополнительный разброс в градусах, который добавляется к minAngle и maxAngle
    /// во время стрельбы по-македонски.
    /// 0 — без штрафа.
    /// Рекомендуемый рабочий диапазон: 0-45.
    /// Чем выше число, тем сильнее уводит выстрелы.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DualWieldInaccuracyPenalty = 0f;

    /// <summary>
    /// Множитель скорострельности только для режима стрельбы по-македонски.
    /// 1 — без бонуса.
    /// 2 — вдвое быстрее.
    /// Практический рабочий диапазон: 1-2.
    /// Итоговая скорострельность всё равно ограничивается dualWieldMaxFireRate.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DualWieldFireRateMultiplier = 1f;

    /// <summary>
    /// Верхний предел скорострельности в режиме стрельбы по-македонски.
    /// Итоговая скорострельность оружия в этом режиме не поднимется выше этого значения.
    /// 0 и ниже — без дополнительного ограничения.
    /// Рекомендуемый рабочий диапазон: 1-10.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DualWieldMaxFireRate = 10f;

    /// <summary>
    /// Небольшая задержка между чередующимися выстрелами из разных рук, в секундах.
    /// 0 — переключение между руками без дополнительной задержки.
    /// Рекомендуемый рабочий диапазон: 0.03-0.15.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DualWieldShotDelay = 0.08f;
}
