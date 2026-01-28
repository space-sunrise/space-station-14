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
}
