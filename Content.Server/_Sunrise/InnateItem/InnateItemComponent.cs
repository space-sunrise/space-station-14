using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server._Sunrise.InnateItem
{
    [RegisterComponent]
    public sealed partial class InnateItemComponent : Component
    {
        [ViewVariables(VVAccess.ReadOnly),
         DataField("instantActions", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public List<string?> InstantActions = new();

        [ViewVariables(VVAccess.ReadOnly),
         DataField("worldTargetActions", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public List<string?> WorldTargetActions = new();

        public List<EntityUid> Actions = new();
    }
}
