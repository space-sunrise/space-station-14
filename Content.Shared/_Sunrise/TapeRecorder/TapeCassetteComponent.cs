using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared._Sunrise.TTS;

namespace Content.Shared._Sunrise.TapeRecorder;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTapeRecorderSystem))]
public sealed partial class TapeCassetteComponent : Component
{
    /// <summary>
    /// Maximum recording length.
    /// </summary>
    [DataField]
    public TimeSpan Capacity = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Current tape head position.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan Position;

    /// <summary>
    /// Recorded speech lines.
    /// </summary>
    public List<TapeCassetteRecord> Records = [];

    /// <summary>
    /// Tape spans that have already been used for recording.
    /// </summary>
    public List<TapeCassetteRecordedRange> RecordedRanges = [];
}

/// <summary>
/// A single speech line recorded on a cassette.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct TapeCassetteRecord(
    TimeSpan Time,
    string Speaker,
    string Message,
    ProtoId<TTSVoicePrototype>? VoiceId);

/// <summary>
/// A used tape span that cannot be recorded over.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct TapeCassetteRecordedRange(TimeSpan Start, TimeSpan End);
