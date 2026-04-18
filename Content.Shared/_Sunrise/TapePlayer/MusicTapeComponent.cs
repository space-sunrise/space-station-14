using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.TapePlayer
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class MusicTapeComponent : Component
    {
        [DataField(required: true)]
        public SoundSpecifier Sound;

        [DataField]
        public string SongName = "";

        [DataField, AutoNetworkedField]
        public float SongLengthSeconds;
    }
}
