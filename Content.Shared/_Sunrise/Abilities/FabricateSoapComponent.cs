using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.Abilities;

[RegisterComponent]
public sealed partial class FabricateSoapComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("soapList")]
    public List<string> SoapList = new()
    {
        "Soap"
    };

    [ViewVariables(VVAccess.ReadWrite),
     DataField("actionFabricateSoap", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionFabricateSoap = "FabricateSoap";
}


public sealed partial class FabricateSoapActionEvent : InstantActionEvent {}
