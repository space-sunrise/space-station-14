namespace Content.Shared._Sunrise.Biocode;

/// <summary>
/// Raised when attempting to throw an entity to check if the user can throw it based on biocode restrictions.
/// </summary>
[ByRefEvent]
public struct AttemptThrowBiocodeEvent
{
    public EntityUid ItemUid;
    public EntityUid? User;

    public bool Cancelled = false;

    public AttemptThrowBiocodeEvent(EntityUid itemUid, EntityUid? user)
    {
        ItemUid = itemUid;
        User = user;
    }
}

