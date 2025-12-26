using Content.Server.Sunrise.GasRegeneration;
using Content.Shared.Atmos;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Sunrise.GasRegeneration;

[RegisterComponent]
[Access(typeof(GasRegenerationSystem))]
public sealed partial class GasRegenerationComponent : Component
{
    [DataField("airRegenerate")] public GasMixture AirRegen { get; set; } = new GasMixture();

    [DataField("duration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);

    [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextRegenTime = TimeSpan.FromSeconds(0);
}
