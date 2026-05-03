using Robust.Shared.Audio;

namespace Content.Shared.Radio.Components;

/// <summary>
/// Entities with <see cref="TelecomServerComponent"/> are needed to transmit messages using headsets.
/// They also need to be powered by <see cref="ApcPowerReceiverComponent"/>
/// have <see cref="EncryptionKeyHolderComponent"/> and filled with encryption keys
/// of channels in order for them to work on the same map as server.
/// </summary>
[RegisterComponent]
public sealed partial class TelecomServerComponent : Component
{
    // Sunrise-Start
    [DataField]
    public int MaxBandwidth = 50;

    [ViewVariables(VVAccess.ReadWrite)]
    public float CurrentLoad = 0f;

    [DataField]
    public float LoadDecayRate = 1f;

    [DataField]
    public float HeatPerMessage = 5000f;

    [DataField]
    public float MaxTemperature = 390f;

    [DataField]
    public float HysteresisTemperature = 360f;

    [ViewVariables]
    public bool Overheated = false;

    [ViewVariables]
    public float AlarmTimer = 0f;

    [DataField]
    public float AlarmInterval = 3f;

    [DataField]
    public SoundSpecifier? OverheatSound = new SoundPathSpecifier("/Audio/_Sunrise/Effects/beeps.ogg");
    // Sunrise-End
}
