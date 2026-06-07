using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    /// Эти переменные управляют модификаторами цен разных газов.
    /// Если для газа нет отдельного модификатора, используется цена по умолчанию из прототипа.
    /// </summary>

    public static readonly CVarDef<float> DefaultGasPriceModifier =
        CVarDef.Create("atmos.gas_price_modifier_default", 1f, CVar.SERVER);

    public static readonly CVarDef<float> GasPriceModifierTritium =
        CVarDef.Create("atmos.gas_price_modifier_tritium", 2.5f, CVar.SERVER);

    public static readonly CVarDef<float> GasPriceModifierNitrousOxide =
        CVarDef.Create("atmos.gas_price_modifier_nitrous_oxide", 0.1f, CVar.SERVER);

    public static readonly CVarDef<float> GasPriceModifierFrezon =
        CVarDef.Create("atmos.gas_price_modifier_frezon", 1f, CVar.SERVER);

    public static readonly CVarDef<float> GasPriceModifierBZ =
        CVarDef.Create("atmos.gas_price_modifier_bz", 3f, CVar.SERVER);

    public static readonly CVarDef<float> GasPriceModifierHealium =
        CVarDef.Create("atmos.gas_price_modifier_healium", 12f, CVar.SERVER);

    public static readonly CVarDef<float> GasPriceModifierNitrium =
        CVarDef.Create("atmos.gas_price_modifier_nitrium", 12f, CVar.SERVER);
}
