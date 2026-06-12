using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Silicons.StationAi;

/// <summary>
/// Marks a borg chassis prepared as a station AI body through an AI communication board.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationAiBodyComponent : Component
{
    /// <summary>
    /// Round-local visible number of this AI body.
    /// </summary>
    [AutoNetworkedField]
    public int BodyNumber;

    /// <summary>
    /// Communication board currently installed into the chassis brain slot.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? Board;

    /// <summary>
    /// Station AI brain currently controlling this body, or null while the body is free.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? LinkedAi;

    /// <summary>
    /// Granted action entity used by the controlled body to reopen the body selector UI.
    /// </summary>
    public EntityUid? BodyMenuAction;

    /// <summary>
    /// Granted action entity used by the controlled body to return control to the AI brain.
    /// </summary>
    public EntityUid? BodyExitAction;
}

[Serializable, NetSerializable]
public enum StationAiBodyVisuals : byte
{
    /// <summary>
    /// Selected AI body appearance layer data.
    /// </summary>
    BodyAppearance,
}
