using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Shower;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShowerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsActive = false;

    [AutoNetworkedField]
    public float Accumulator = 0f;

    [AutoNetworkedField]
    public EntityUid? CurrentPuddle;
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