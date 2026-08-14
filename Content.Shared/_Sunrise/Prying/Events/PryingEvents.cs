#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Prying.Components;

/// <summary>
/// Raised directed on the user before they attempt to pry a target.
/// Cancel to stop the pry before target-side pry validation runs.
/// </summary>
[ByRefEvent]
public record struct UserBeforePryEvent(EntityUid Target, bool PryPowered, bool Force, bool StrongPry)
{
    public string? Message;

    public bool Cancelled;
}

/// <summary>
/// Raised directed on the user after they successfully pried a door.
/// </summary>
[ByRefEvent]
public readonly record struct UserPriedDoorEvent(EntityUid Door, bool Opened);
