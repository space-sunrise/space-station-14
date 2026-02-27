using Content.Shared.Objectives.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.PlanetPrison;

/// <summary>
/// Requires that the player is alive and not restrained (no handcuffs or straightjacket).
/// </summary>
[RegisterComponent, Access(typeof(StayFreeConditionSystem))]
public sealed partial class StayFreeConditionComponent : Component
{
    /// <summary>
    /// Оригинальная иконка цели, сохранённая перед заменой на иконку наручников
    /// </summary>
    [DataField]
    public SpriteSpecifier? OriginalIcon;

    /// <summary>
    /// Флаг, указывающий, была ли иконка заменена на иконку наручников
    /// </summary>
    [DataField]
    public bool IconOverridden;

    /// <summary>
    /// Иконка, которая отображается, когда игрок закован (наручники/смирительная рубашка)
    /// </summary>
    [DataField]
    public SpriteSpecifier RestrainedIcon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Alerts/Handcuffed/Handcuffed.png"));
}

