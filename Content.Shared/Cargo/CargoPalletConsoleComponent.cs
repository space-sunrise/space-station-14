using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Cargo;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedCargoSystem))]
public sealed partial class CargoPalletConsoleComponent : Component
{
    [DataField]
    public SoundSpecifier ErrorSound = new SoundCollectionSpecifier("CargoError");

    /// <summary>
    /// The time at which the console will be able to play the deny sound.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDenySoundTime = TimeSpan.Zero;

    /// <summary>
    /// The time between playing the deny sound.
    /// </summary>
    [DataField]
    public TimeSpan DenySoundDelay = TimeSpan.FromSeconds(2);
}
