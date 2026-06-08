using Content.Shared.Containers.ItemSlots;
using Content.Shared._Sunrise.TTS;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Sunrise.TapeRecorder;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedTapeRecorderSystem))]
public sealed partial class TapeRecorderComponent : Component
{
    public const string CassetteSlotId = "cassette";

    /// <summary>
    /// Current tape recorder mode.
    /// </summary>
    [AutoNetworkedField]
    public TapeRecorderMode Mode = TapeRecorderMode.Stopped;

    /// <summary>
    /// Cassette currently inserted into the recorder.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? InsertedCassette;

    /// <summary>
    /// Slot that accepts recordable cassettes.
    /// </summary>
    [DataField(required: true)]
    public ItemSlot CassetteSlot = new();

    /// <summary>
    /// Listener range while the recorder is recording.
    /// </summary>
    [DataField]
    public float RecordingRange = 7f;

    /// <summary>
    /// Tape rewind speed multiplier.
    /// </summary>
    [DataField]
    public float RewindSpeed = 12f;

    /// <summary>
    /// Delay between generated TTS playback lines.
    /// </summary>
    [DataField]
    public TimeSpan PlaybackLineCooldown = TimeSpan.FromSeconds(0.4);

    /// <summary>
    /// Voice used for cassette playback.
    /// </summary>
    [DataField]
    public ProtoId<TTSVoicePrototype> PlaybackVoice = "Skippy";

    /// <summary>
    /// Paper prototype spawned by transcript printing.
    /// </summary>
    [DataField]
    public EntProtoId PaperPrototype = "TapeRecorderTranscript";

    /// <summary>
    /// Cooldown between transcript prints.
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Next time when transcript printing is allowed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPrintTime;

    /// <summary>
    /// Sound played when changing recorder mode.
    /// </summary>
    [DataField]
    public SoundSpecifier? ButtonSound = new SoundPathSpecifier("/Audio/_Sunrise/TapePlayer/switch.ogg");

    /// <summary>
    /// Sound played when a cassette is printed to paper.
    /// </summary>
    [DataField]
    public SoundSpecifier? PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(3f)
    };

    /// <summary>
    /// Fallback speaker name when the original source cannot be named.
    /// </summary>
    [DataField]
    public LocId UnknownSpeaker = "tape-recorder-speaker-unknown";

    /// <summary>
    /// Last server time used for active mode simulation.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan LastUpdateTime;

    /// <summary>
    /// Next time when playback may emit a TTS line.
    /// </summary>
    public TimeSpan NextPlaybackLineTime;
}

[Serializable, NetSerializable]
public sealed class TapeRecorderSetModeMessage(TapeRecorderMode mode) : BoundUserInterfaceMessage
{
    public TapeRecorderMode Mode { get; } = mode;
}

[Serializable, NetSerializable]
public sealed class TapeRecorderPrintMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class TapeRecorderBoundUserInterfaceState(
    string cassetteName,
    TapeRecorderMode mode,
    float positionSeconds,
    float capacitySeconds,
    int recordCount,
    bool hasCassette) : BoundUserInterfaceState
{
    public string CassetteName { get; } = cassetteName;
    public TapeRecorderMode Mode { get; } = mode;
    public float PositionSeconds { get; } = positionSeconds;
    public float CapacitySeconds { get; } = capacitySeconds;
    public int RecordCount { get; } = recordCount;
    public bool HasCassette { get; } = hasCassette;
}

[Serializable, NetSerializable]
public enum TapeRecorderMode : byte
{
    Stopped,
    Recording,
    Playing,
    Rewinding
}

[Serializable, NetSerializable]
public enum TapeRecorderUiKey : byte
{
    Key
}
