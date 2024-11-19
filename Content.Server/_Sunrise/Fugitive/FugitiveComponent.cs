using Robust.Shared.Audio;

namespace Content.Server._Sunrise.Fugitive
{
    [RegisterComponent]
    public sealed partial class FugitiveComponent : Component
    {
        [DataField("spawnSound")]
        public SoundSpecifier SpawnSoundPath = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

        [DataField("firstMindAdded")]
        public bool FirstMindAdded = false;

        [DataField("roundStart")]
        public bool RoundStart = false;
    }
}
