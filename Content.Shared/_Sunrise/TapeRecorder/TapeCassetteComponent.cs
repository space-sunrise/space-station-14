using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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
}

/// <summary>
/// A single speech line recorded on a cassette.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct TapeCassetteRecord(TimeSpan Time, string Speaker, string Message);
