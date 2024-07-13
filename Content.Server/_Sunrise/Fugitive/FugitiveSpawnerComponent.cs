using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.Fugitive
{
    [RegisterComponent]
    public sealed partial class FugitiveSpawnerComponent : Component
    {
        [DataField("spawnSound")]
        public SoundSpecifier SpawnSoundPath = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

        [ViewVariables(VVAccess.ReadWrite),
         DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Prototype = "MobHumanFugitive";

        public List<string> Implants = new() { "UplinkImplant", "FreedomImplant"};
    }
}
