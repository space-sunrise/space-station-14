using Content.Shared._Sunrise.Mood;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Fish.Mood;

/// <summary>
/// Sets which sprite RSI is used for displaying the mood visuals and what state to use based on the current mood threshold.
/// </summary>
[RegisterComponent]
public sealed partial class MoodVisualsComponent : Component
{
    /// <summary>
    /// Sprite RSI used for mood visualization.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Sprite;

    /// <summary>
    /// Dictionary mapping mood thresholds to sprite states.
    /// If a threshold is not in this dictionary, no sprite will be shown for that threshold.
    /// </summary>
    [DataField]
    public Dictionary<MoodThreshold, string> MoodStates = new();
}

