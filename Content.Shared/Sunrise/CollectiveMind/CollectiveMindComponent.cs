using Content.Shared._Sunrise.CollectiveMind;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Sunrise.CollectiveMind
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class CollectiveMindComponent : Component
    {
        [DataField("minds", customTypeSerializer: typeof(PrototypeIdListSerializer<CollectiveMindPrototype>))]
        public List<string> Minds = new();
    }
}
