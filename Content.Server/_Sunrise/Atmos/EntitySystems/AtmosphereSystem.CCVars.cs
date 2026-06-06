using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;

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

    private IDisposable? _configSub;

    public void InitSunriseAtmosCVars()
    {
        if (_configSub is not null)
        {
            return;
        }

        _configSub = _cfg.SubscribeMultiple()
            .OnValueChanged(SunriseCCVars.DefaultGasPriceModifier, (value) => _defaultGasPriceModifier = value, true)
            .OnValueChanged(SunriseCCVars.GasPriceModifierTritium, (value) => _gasPriceModifierTritium = value, true)
            .OnValueChanged(SunriseCCVars.GasPriceModifierNitrousOxide, (value) => _gasPriceModifierNitrousOxide = value, true)
            .OnValueChanged(SunriseCCVars.GasPriceModifierFrezon, (value) => _gasPriceModifierFrezon = value, true)
            .OnValueChanged(SunriseCCVars.GasPriceModifierBZ, (value) => _gasPriceModifierBZ = value, true)
            .OnValueChanged(SunriseCCVars.GasPriceModifierHealium, (value) => _gasPriceModifierHealium = value, true)
            .OnValueChanged(SunriseCCVars.GasPriceModifierNitrium, (value) => _gasPriceModifierNitrium = value, true);
    }

    public float GetModifier(string id)
    {
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

    private void ShutdownSunriseAtmosCVars()
    {
        _configSub?.Dispose();
    }
}
