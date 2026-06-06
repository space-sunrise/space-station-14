using Content.Client._Sunrise.Tutorial.ProgressBar;

namespace Content.Client._Sunrise.Tutorial.Components;

/// <summary>
/// Client-only link between a tutorial entity and its progress bar control.
/// </summary>
[RegisterComponent]
public sealed partial class ProgressBarUiComponent : Component
{
    /// <summary>
    /// Progress bar control currently attached to the viewport.
    /// </summary>
    public TutorialProgressBar? Bar;
}
