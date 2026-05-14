using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private void InitializeCVars()
    {
        _cfg.OnValueChanged(
            CCVars.AtmosHeatScale,
            v => HeatScale = MathF.Max(0.000001f, v),
            invokeImmediately: true);
    }
}
