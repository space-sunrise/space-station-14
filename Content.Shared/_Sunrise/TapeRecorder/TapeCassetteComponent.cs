using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.TapeRecorder;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTapeRecorderSystem))]
public sealed partial class TapeCassetteComponent : Component
{
    /// <summary>
    /// Maximum recording length in seconds.
    /// </summary>
    [DataField]
    public float CapacitySeconds = 60f;

    /// <summary>
    /// Current tape head position in seconds.
    /// </summary>
    [AutoNetworkedField]
    public float PositionSeconds;

    /// <summary>
    /// Recorded speech lines.
    /// </summary>
    [AutoNetworkedField]
    public List<TapeCassetteRecord> Records = [];
}

/// <summary>
/// A single speech line recorded on a cassette.
/// </summary>
[Serializable, NetSerializable]
public sealed class TapeCassetteRecord
{
    /// <summary>
    /// Tape position in seconds where the speech line starts.
    /// </summary>
    public float Time { get; set; }

    /// <summary>
    /// Display name of the speaker that was recorded.
    /// </summary>
    public string Speaker { get; set; } = string.Empty;

    /// <summary>
    /// Recorded speech text.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
