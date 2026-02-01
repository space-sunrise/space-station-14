using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Atmos.Components;
using Robust.Shared.Configuration;

namespace Content.Server.Atmos.EntitySystems;

public enum WindowID
{
    // Reinforced plasma windows
    PlasmaReinforcedWindowDirectional,
    ReinforcedPlasmaWindow,
    ReinforcedPlasmaWindowDiagonal,

    // Reinforced windows
    WindowReinforcedDirectional,
    ReinforcedWindow,
    ReinforcedWindowDiagonal
}

// This system allows to override values from `deltapressure.yml` without modifying prototypes
public partial class DeltaPressureSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;


    private float _minPReinforcedPlasma;
    private float _deltaPReinforcedPlasma;

    private float _minPReinforcedPlasmaQuarter;
    private float _deltaPReinforcedPlasmaQuarter;

    private float _minPReinforced;
    private float _deltaPReinforced;

    private float _minPReinforcedQuarter;
    private float _deltaPReinforcedQuarter;

    partial void AfterInit()
    {
        _cfg.OnValueChanged(SunriseCCVars.MinPReinforcedPlasma, (value) => _minPReinforcedPlasma = value, true);
        _cfg.OnValueChanged(SunriseCCVars.DeltaPReinforcedPlasma, (value) => _deltaPReinforcedPlasma = value, true);

        _cfg.OnValueChanged(SunriseCCVars.MinPReinforcedPlasmaQuarter, (value) => _minPReinforcedPlasmaQuarter = value, true);
        _cfg.OnValueChanged(SunriseCCVars.DeltaPReinforcedPlasmaQuarter, (value) => _deltaPReinforcedPlasmaQuarter = value, true);

        _cfg.OnValueChanged(SunriseCCVars.MinPReinforced, (value) => _minPReinforced = value, true);
        _cfg.OnValueChanged(SunriseCCVars.DeltaPReinforced, (value) => _deltaPReinforced = value, true);

        _cfg.OnValueChanged(SunriseCCVars.MinPReinforcedQuarter, (value) => _minPReinforcedQuarter = value, true);
        _cfg.OnValueChanged(SunriseCCVars.DeltaPReinforcedQuarter, (value) => _deltaPReinforcedQuarter = value, true);

        SubscribeLocalEvent<DeltaPressureComponent, ComponentStartup>(OnDeltaPressureComponentStartup);
    }

    private void OnDeltaPressureComponentStartup(Entity<DeltaPressureComponent> ent, ref ComponentStartup args)
    {
        Override(ent, ref args);
    }

    private bool Override(Entity<DeltaPressureComponent> ent, ref ComponentStartup args)
    {
        TryPrototype(ent, out var proto);

        if (proto is null || !Enum.TryParse<WindowID>(proto.ID, out var windowProtoID))
            return false;

        return windowProtoID switch
        {
            WindowID.PlasmaReinforcedWindowDirectional => SetPressure(ent, _minPReinforcedPlasmaQuarter, _deltaPReinforcedPlasmaQuarter),
            WindowID.ReinforcedPlasmaWindow or WindowID.ReinforcedPlasmaWindowDiagonal => SetPressure(ent, _minPReinforcedPlasma, _deltaPReinforcedPlasma),
            WindowID.WindowReinforcedDirectional => SetPressure(ent, _minPReinforcedQuarter, _deltaPReinforcedQuarter),
            WindowID.ReinforcedWindow or WindowID.ReinforcedWindowDiagonal => SetPressure(ent, _minPReinforced, _deltaPReinforced),
            _ => false,
        };
    }

    private static bool SetPressure(Entity<DeltaPressureComponent> ent, float minP, float deltaP)
    {
        ent.Comp.MinPressure = minP;
        ent.Comp.MinPressureDelta = deltaP;
        return true;
    }
}
