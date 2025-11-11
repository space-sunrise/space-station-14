using Content.Shared._Sunrise.Mood;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Client._Sunrise.Mood;

/// <summary>
/// Sets which sprite RSI is used for displaying the mood visuals and what state to use based on the current mood threshold.
/// </summary>
[RegisterComponent]
public sealed partial class MoodVisualsComponent : Component
{
    /// <summary>
    /// Path to the RSI sprite used for mood visualization.
    /// </summary>
    [DataField]
    public string? Sprite;

    /// <summary>
    /// Dictionary mapping mood thresholds to sprite states.
    /// If a threshold is not in this dictionary, no sprite will be shown for that threshold.
    /// </summary>
    [DataField(customTypeSerializer: typeof(DictionarySerializer<MoodThreshold, string>))]
    public Dictionary<MoodThreshold, string> MoodStates = new();
}
