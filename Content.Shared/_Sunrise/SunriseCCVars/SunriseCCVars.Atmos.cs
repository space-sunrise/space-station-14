using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    /// These variables control modifications of various gas prices. If gas has no specified
    /// modifier here, it will use default price from prototype
    /// </summary>

    public static readonly CVarDef<float> DefaultGasPriceModifier =
        CVarDef.Create("atmos.gas_price_modifier_default", 1f, CVar.SERVER);

    public static readonly CVarDef<float> GasPriceModifierTritium =
        CVarDef.Create("atmos.gas_price_modifier_tritium", 0.016f, CVar.SERVER);

    public static readonly CVarDef<float> GasPriceModifierNitrousOxide =
        CVarDef.Create("atmos.gas_price_modifier_nitrous_oxide", 2f, CVar.SERVER);

    public static readonly CVarDef<float> GasPriceModifierFrezon =
        CVarDef.Create("atmos.gas_price_modifier_frezon", 0.25f, CVar.SERVER);

    public static readonly CVarDef<float> GasPriceModifierBZ =
        CVarDef.Create("atmos.gas_price_modifier_bz", 1f, CVar.SERVER);

    public static readonly CVarDef<float> GasPriceModifierHealium =
        CVarDef.Create("atmos.gas_price_modifier_healium", 1f, CVar.SERVER);

    public static readonly CVarDef<float> GasPriceModifierNitrium =
        CVarDef.Create("atmos.gas_price_modifier_nitrium", 1f, CVar.SERVER);

    /// <summary>
    /// DeltaP threshold overrides for pressure window shattering
    /// </summary>

    public static readonly CVarDef<float> MinPReinforcedPlasma =
        CVarDef.Create("atmos.reinforced_plasma_window_minP", 150000f, CVar.SERVER);

    public static readonly CVarDef<float> DeltaPReinforcedPlasma =
        CVarDef.Create("atmos.reinforced_plasma_window_deltaP", 100000f, CVar.SERVER);

    public static readonly CVarDef<float> MinPReinforcedPlasmaQuarter =
        CVarDef.Create("atmos.reinforced_plasma_window_quarter_minP", 37500f, CVar.SERVER);

    public static readonly CVarDef<float> DeltaPReinforcedPlasmaQuarter =
        CVarDef.Create("atmos.reinforced_plasma_window_quarter_deltaP", 25000f, CVar.SERVER);

    public static readonly CVarDef<float> MinPReinforced =
        CVarDef.Create("atmos.reinforced_window_minP", 15000f, CVar.SERVER);

    public static readonly CVarDef<float> DeltaPReinforced =
        CVarDef.Create("atmos.reinforced_window_deltaP", 10000f, CVar.SERVER);

    public static readonly CVarDef<float> MinPReinforcedQuarter =
        CVarDef.Create("atmos.reinforced_window_quarter_minP", 3750f, CVar.SERVER);

    public static readonly CVarDef<float> DeltaPReinforcedQuarter =
        CVarDef.Create("atmos.reinforced_window_quarter_deltaP", 2500f, CVar.SERVER);
}
