using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Mech;

/// <summary>
/// Накладывается на мех, когда он находится под действием электромагнитного импульса
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MechOnEMPPulseComponent : Component
{
    [ViewVariables]
    public TimeSpan EffectInterval = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public TimeSpan NextEffectTime;

}
