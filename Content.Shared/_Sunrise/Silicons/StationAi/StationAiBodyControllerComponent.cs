using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Silicons.StationAi;

/// <summary>
/// Tracks which station AI body is currently controlled by this AI brain.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationAiBodyControllerComponent : Component
{
    /// <summary>
    /// Current borg chassis controlled by the AI, or null while the AI is in its regular brain/core.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? CurrentBody;

    /// <summary>
    /// Granted action entity used by the AI brain to open the body selector UI.
    /// </summary>
    public EntityUid? BodyMenuAction;
}
