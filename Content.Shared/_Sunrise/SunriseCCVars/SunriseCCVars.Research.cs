using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    /// Целевая популяция для скейлинга очков исследований.
    /// </summary>
    public static readonly CVarDef<int> ResearchPointScalingTargetPopulation =
        CVarDef.Create("research.point_scaling_target_population", 44, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Задержка в минутах перед фиксацией модификатора скейлинга очков исследований.
    /// </summary>
    public static readonly CVarDef<int> ResearchPointScalingDelayMinutes =
        CVarDef.Create("research.point_scaling_delay_minutes", 3, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Сила скейлинга очков исследований от онлайна.
    /// 0 -> отключает модификатор, стандарт.
    /// Чем выше значение, тем сильнее бонус при низком онлайне и штраф при высоком.
    /// </summary>
    public static readonly CVarDef<float> ResearchPointScalingMultiplier =
        CVarDef.Create("research.point_scaling_multiplier", 1.15f, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Минимальный модификатор скейлинга очков исследований по онлайну.
    /// </summary>
    public static readonly CVarDef<float> ResearchPointScalingMinModifier =
        CVarDef.Create("research.point_scaling_min_modifier", 0.6f, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Максимальный модификатор скейлинга очков исследований по онлайну.
    /// </summary>
    public static readonly CVarDef<float> ResearchPointScalingMaxModifier =
        CVarDef.Create("research.point_scaling_max_modifier", 1.5f, CVar.SERVER | CVar.ARCHIVE);
}
