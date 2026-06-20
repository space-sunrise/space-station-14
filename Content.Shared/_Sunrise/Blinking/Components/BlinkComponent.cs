using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Sunrise.Blinking;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(BlinkSystem))]
public sealed partial class BlinkComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextBlinkTime;

    [DataField]
    public TimeSpan MinBlinkDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan MaxBlinkDelay = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}

[Serializable, NetSerializable]
public enum BlinkVisuals : byte
{
    EyesClosed
}
