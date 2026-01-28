using Content.Shared._Sunrise.SunriseCCVars;

namespace Content.Server.Atmos.EntitySystems;

public enum GasIds
{
    Tritium,
    NitrousOxide,
    Frezon,
    BZ,
    Healium,
    Nitrium
}

public partial class AtmosphereSystem
{
    private float _defaultGasPriceModifier;
    private float _gasPriceModifierTritium;
    private float _gasPriceModifierNitrousOxide;
    private float _gasPriceModifierFrezon;
    private float _gasPriceModifierBZ;
    private float _gasPriceModifierHealium;
    private float _gasPriceModifierNitrium;

    private bool _subscribed = false;

    public void Subscribe()
    {
        if (_subscribed)
        {
            return;
        }

        _cfg.OnValueChanged(SunriseCCVars.DefaultGasPriceModifier, (value) => _defaultGasPriceModifier = value, true);
        _cfg.OnValueChanged(SunriseCCVars.GasPriceModifierTritium, (value) => _gasPriceModifierTritium = value, true);
        _cfg.OnValueChanged(SunriseCCVars.GasPriceModifierNitrousOxide, (value) => _gasPriceModifierNitrousOxide = value, true);
        _cfg.OnValueChanged(SunriseCCVars.GasPriceModifierFrezon, (value) => _gasPriceModifierFrezon = value, true);
        _cfg.OnValueChanged(SunriseCCVars.GasPriceModifierBZ, (value) => _gasPriceModifierBZ = value, true);
        _cfg.OnValueChanged(SunriseCCVars.GasPriceModifierHealium, (value) => _gasPriceModifierHealium = value, true);
        _cfg.OnValueChanged(SunriseCCVars.GasPriceModifierNitrium, (value) => _gasPriceModifierNitrium = value, true);

        _subscribed = true;
    }

    public float GetModifier(string id)
    {
        Subscribe();

        if (!Enum.TryParse<GasIds>(id, out var gasId))
            return _defaultGasPriceModifier;

        return gasId switch
        {
            GasIds.Tritium => _gasPriceModifierTritium,
            GasIds.NitrousOxide => _gasPriceModifierNitrousOxide,
            GasIds.Frezon => _gasPriceModifierFrezon,
            GasIds.BZ => _gasPriceModifierBZ,
            GasIds.Healium => _gasPriceModifierHealium,
            GasIds.Nitrium => _gasPriceModifierNitrium,
            _ => _defaultGasPriceModifier,
        };
    }
}
