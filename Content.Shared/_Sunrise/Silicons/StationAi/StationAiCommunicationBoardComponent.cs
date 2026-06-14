using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Silicons.StationAi;

/// <summary>
/// Marks an item that turns an empty borg chassis into a free station AI body.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StationAiCommunicationBoardComponent : Component;
