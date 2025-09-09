using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.TTS;

/// <summary>
/// Component for cyborgs that allows them to change their TTS voice.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BorgVoiceComponent : Component
{
    /// <summary>
    /// The currently selected voice prototype ID.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public ProtoId<TTSVoicePrototype>? SelectedVoiceId { get; set; }

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public string VoiceEffect = "robot";
}
