using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.TTS;

/// <summary>
/// Event raised when a cyborg wants to change their TTS voice.
/// </summary>
public sealed partial class BorgVoiceChangeActionEvent : InstantActionEvent
{
}

/// <summary>
/// Event sent from client to server to change borg voice.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BorgVoiceChangeMessage : BoundUserInterfaceMessage
{
    public string VoiceId;

    public BorgVoiceChangeMessage(string voiceId)
    {
        VoiceId = voiceId;
    }
}

/// <summary>
/// Event sent from server to client to update borg voice UI.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BorgVoiceChangeState : BoundUserInterfaceState
{
    public string? CurrentVoiceId;
    public List<string> AvailableVoices;

    public BorgVoiceChangeState(string? currentVoiceId, List<string> availableVoices)
    {
        CurrentVoiceId = currentVoiceId;
        AvailableVoices = availableVoices;
    }
}

[Serializable, NetSerializable]
public enum BorgVoiceUiKey : byte
{
    Key
}
