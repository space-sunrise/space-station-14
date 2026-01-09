using Content.Shared._Sunrise.Mood;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Mood;

/// <summary>
/// Устанавливает, какой спрайт RSI используется для отображения визуальных эффектов настроения и какое состояние использовать в зависимости от текущего порога настроения.
/// </summary>
[RegisterComponent]
public sealed partial class MoodVisualsComponent : Component
{
    /// <summary>
    /// Спрайт RSI, используемый для визуализации настроения.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Sprite;

    /// <summary>
    /// Словарь, сопоставляющий пороги настроения с состояниями спрайта.
    /// Если порог отсутствует в этом словаре, спрайт для этого порога отображаться не будет.
    /// </summary>
    [DataField]
    public Dictionary<MoodThreshold, string> MoodStates = new();
}
