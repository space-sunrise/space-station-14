using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Antags.Vampires.Components;

/// <summary>
/// Святые уязвимости вампира.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class VampireHolyComponent : Component
{
    /// <summary>
    /// Интервал святого урона.
    /// </summary>
    [DataField]
    public TimeSpan EffectInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Радиус святого места.
    /// </summary>
    [DataField]
    public float HolyPlaceRange = 8f;

    /// <summary>
    /// Святая вода.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> HolyWaterReagent = "Holywater";

    /// <summary>
    /// Следующий эффект святой воды.
    /// </summary>
    [AutoPausedField]
    public TimeSpan NextHolyWaterEffect;

    /// <summary>
    /// Следующий эффект святого места.
    /// </summary>
    [AutoPausedField]
    public TimeSpan NextHolyPlaceEffect;

    /// <summary>
    /// Следующее предупреждение.
    /// </summary>
    [AutoPausedField]
    public TimeSpan NextHolyPlacePopup;
}
