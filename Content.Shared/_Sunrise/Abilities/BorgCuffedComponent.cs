using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.Abilities;

[RegisterComponent]
public sealed partial class BorgCuffedComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite),
     DataField("cableCuffs", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CableCuffsId = "Cablecuffs";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("cuffActionId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CuffActionId = "BorgCuffed";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("cuffTime")]
    public float CuffTime = 3.5f;
}


public sealed partial class BorgCuffedActionEvent : EntityTargetActionEvent
{

}

[Serializable, NetSerializable]
public sealed partial class BorgCuffedDoAfterEvent : SimpleDoAfterEvent
{

}
