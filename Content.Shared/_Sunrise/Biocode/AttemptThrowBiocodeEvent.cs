namespace Content.Shared._Sunrise.Biocode;

/// <summary>
/// Raised when attempting to throw an entity to check if the user can throw it based on biocode restrictions.
/// </summary>
[ByRefEvent]
public struct AttemptThrowBiocodeEvent(EntityUid itemUid, EntityUid? user)
{
    public readonly EntityUid ItemUid = itemUid;
    public readonly EntityUid? User = user;
    public bool Cancelled;
}
