using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Tutorial.Components;

/// <summary>
/// Networked marker for an entity that should display a tutorial speech bubble.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TutorialBubbleComponent : Component
{
    /// <summary>
    /// Localization key for the instruction shown in the bubble.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Instruction;
}
