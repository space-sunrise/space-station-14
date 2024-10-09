using Content.Server.Chat.Systems;
using Robust.Shared.Enums;

namespace Content.Server._Sunrise.Chat;

/// <summary>
/// Makes this entity periodically advertise by speaking a randomly selected
/// message from a specified dataset into local chat.
/// </summary>
[RegisterComponent, Access(typeof(SayWithIntervalSystem))]
public sealed partial class SayWithIntervalComponent : Component
{
    /// <summary>
    /// Minimum time in seconds to wait before saying a new ad, in seconds. Has to be larger than or equal to 1.
    /// </summary>
    [DataField]
    public int MinimumWait { get; private set; } = 20;

    /// <summary>
    /// Maximum time in seconds to wait before saying a new ad, in seconds. Has to be larger than or equal
    /// to <see cref="MinimumWait"/>
    /// </summary>
    [DataField]
    public int MaximumWait { get; private set; } = 40;


    /// <summary>
    /// The identifier for the advertisements dataset prototype.
    /// </summary>
    [DataField] public string Message = "123";

    [DataField] public string chatType = "speak";
    [DataField] public bool Format = false;

    /// <summary>
    /// The next time an advertisement will be said.
    /// </summary>
    [DataField]
    public TimeSpan NextMessageTime { get; set; } = TimeSpan.Zero;

}
