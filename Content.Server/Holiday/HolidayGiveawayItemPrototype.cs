using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Holiday
{
    [Prototype("holidayGiveawayItem")]
    public sealed partial class HolidayGiveawayItemPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("holiday", customTypeSerializer:typeof(PrototypeIdSerializer<HolidayPrototype>))]
        public string Holiday { get; private set; } = string.Empty;

        [DataField("prototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Prototype { get; private set; } = string.Empty;
    }
}
