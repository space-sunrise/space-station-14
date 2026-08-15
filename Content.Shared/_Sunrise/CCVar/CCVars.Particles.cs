using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Controls particle effect quality.
    /// 0 = Off, 1 = Low, 2 = Medium, 3 = High
    /// Low:    25% of maxCount per emitter
    /// Medium: 50% of maxCount per emitter
    /// High:   100% of maxCount per emitter
    ///
    /// Particles with IgnoreQualitySettings bypass the quality multiplier but still respect their safety caps.
    /// </summary>
    public static readonly CVarDef<int> ParticleQuality =
        CVarDef.Create("particles.quality", 3, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Maximum total number of live cosmetic particles across all emitters.
    /// Emitters will reduce their emission rate once the budget is exhausted.
    /// ParticleQuality presets cap this value below High; gameplay-critical effects retain a bounded minimum budget.
    /// </summary>
    public static readonly CVarDef<int> ParticleGlobalBudget =
        CVarDef.Create("particles.global_budget", 8000, CVar.CLIENTONLY | CVar.ARCHIVE);
}

