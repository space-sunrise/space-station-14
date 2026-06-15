using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Sunrise.Shared.Shower;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShowerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsActive = false;
}

[Serializable, NetSerializable]
public enum ShowerVisuals : byte
{
    Active
}

[Serializable, NetSerializable]
public enum ShowerVisualLayers : byte
{
    Base,
    Water
}