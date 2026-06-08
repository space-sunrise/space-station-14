using Robust.Shared.GameStates;

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
    public List<TapeCassetteRecord> Records = [];
}

public sealed class TapeCassetteRecord
{
    public float Time;
    public string Speaker = string.Empty;
    public string Message = string.Empty;
}
