using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server._Sunrise.StationGoal
{
    [Serializable, Prototype("stationGoal")]
    public sealed class StationGoalPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; } = default!;

        [DataField("text")]
        public string Text { get; set; } = string.Empty;

        // Sunrise-start
        [ViewVariables(VVAccess.ReadWrite),
         DataField("lockBoxPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string LockBoxPrototypeId = "LockboxCaptain";

        [ViewVariables(VVAccess.ReadOnly),
         DataField("extraItems", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public List<string?> ExtraItems = new();
        // Sunrise-end
    }
}
