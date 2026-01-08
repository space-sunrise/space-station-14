using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.PDA;

/// <summary>
/// Компонент для настройки переключения между статичным и анимированным состояниями PDA
/// в зависимости от наличия ID карты.
/// </summary>
[RegisterComponent]
public sealed partial class PdaAnimationVisualsComponent : Component
{
    /// <summary>
    /// Конфигурация анимации для PDA.
    /// Если не задана, используется стандартное поведение без переключения состояний.
    /// </summary>
    [DataField]
    public PdaAnimationConfig? AnimationConfig;
}

/// <summary>
/// Конфигурация для переключения между статичным и анимированным состояниями PDA.
/// </summary>
[DataDefinition]
public sealed partial class PdaAnimationConfig
{
    /// <summary>
    /// Имя state для статичного отображения (когда ID карта вынута).
    /// Если не указан, будет использован первый кадр из AnimatedState с остановленной анимацией.
    /// </summary>
    [DataField]
    public string? StaticState;

    /// <summary>
    /// Имя state для анимированного отображения (когда ID карта вставлена).
    /// </summary>
    [DataField(required: true)]
    public string AnimatedState = default!;

    /// <summary>
    /// Имя state для слоя индикации вставленной ID карты.
    /// </summary>
    [DataField(required: true)]
    public string IdInsertedLayerState = default!;
}
