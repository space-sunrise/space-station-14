using Content.Client._Sunrise.Tutorial.TutorialBubbleControl;

namespace Content.Client._Sunrise.Tutorial.Components;

/// <summary>
/// Client-only link between a tutorial bubble entity and its UI control.
/// </summary>
[RegisterComponent]
public sealed partial class TutorialBubbleUiComponent : Component
{
    /// <summary>
    /// Bubble control currently attached to the viewport.
    /// </summary>
    public TutorialBubble? Bubble;

    /// <summary>
    /// Last localization key shown in <see cref="Bubble"/>.
    /// Used to avoid restarting the fade-in animation when unrelated component fields change.
    /// </summary>
    public string? LastInstruction;
}
