using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Ligyb;

[RegisterComponent]
public sealed partial class MinimumBleedComponent : Component
{
    [DataField] public float MinValue = 1f;
}
