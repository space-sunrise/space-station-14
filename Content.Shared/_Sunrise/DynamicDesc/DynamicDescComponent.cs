using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.DynamicDesc;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DynamicDescComponent : Component
{
    [DataField]
    public string Content = string.Empty;

    [DataField]
    public EntProtoId DynamicDescChangeAction = "DynamicDescChangeAction";

    [DataField, AutoNetworkedField]
    public EntityUid? DynamicDescChangeActionEntity;
}
