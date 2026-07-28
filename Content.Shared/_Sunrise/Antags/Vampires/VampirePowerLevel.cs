using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Antags.Vampires;

/// <summary>
/// Уровень силы вампира. Порядок значений определяет порядок прогрессии
/// </summary>
public enum VampirePowerLevel : byte
{
    Neonate,    // Начальный уровень силы
    Awakened,
    Nightborn,
    Ancient,    // Максимальный уровень силы, который может быть достигнут через накопленную кровь
    Ascendant,  // КРОВЬ НЕ ДОЛЖНА ОТКРЫВАТЬ ДАННЫЙ УРОВЕНЬ СИЛЫ
                // Сейчас не используется, считается "максимальным уровнем силы", который может быть достигнут через специальные цели
    Absolute,   // КРОВЬ НЕ ДОЛЖНА ОТКРЫВАТЬ ДАННЫЙ УРОВЕНЬ СИЛЫ
                // Сейчас не используется, только для админов, НЕ ИСПОЛЬЗОВАТЬ в качестве уровня силы в геймплее
}

/// <summary>
/// Настройки автоматического достижения уровня силы вампира.
/// Отсутствующий <see cref="RequiredTotalBlood"/> запрещает открывать уровень выпитой кровью.
/// </summary>
[Prototype]
public sealed partial class VampirePowerLevelPrototype : IPrototype
{
    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Уровень силы...
    /// </summary>
    [DataField(required: true)]
    public VampirePowerLevel Level;

    /// <summary>
    /// Сколько всего крови нужно выпить для автоматического достижения уровня.
    /// </summary>
    [DataField]
    public int? RequiredTotalBlood;
}
