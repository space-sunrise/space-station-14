using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.Abilities;

[RegisterComponent]
public sealed partial class FabricateCandyComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite),
     DataField("foodGumballId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string FoodGumballId = "FoodGumball";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("foodLollipopId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string FoodLollipopId = "FoodLollipop";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("actionFabricateLollipop", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionFabricateLollipop = "FabricateLollipop";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("actionFabricateGumball", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionFabricateGumball = "FabricateGumball";
}



public sealed partial class FabricateLollipopActionEvent : InstantActionEvent {}

public sealed partial class FabricateGumballActionEvent : InstantActionEvent {}
