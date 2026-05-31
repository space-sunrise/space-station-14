using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Tutorial.Components;

/// <summary>
/// Networked tutorial progress state displayed by the client-side progress bar.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TutorialProgressBarComponent : Component
{
    /// <summary>
    /// Index of the active tutorial step in the current sequence.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentStepIndex;
}
