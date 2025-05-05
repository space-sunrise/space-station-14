using Content.Shared.Charges.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Charges.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedChargesSystem))]
public sealed partial class DeleteWithoutChargesComponent : Component
{
}
