using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Handles the static overlay for station AI.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState] // Starlight-Surgery-edit
public sealed partial class StationAiOverlayComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool AllowCrossGrid; // Starlight-Surgery-edit
}

