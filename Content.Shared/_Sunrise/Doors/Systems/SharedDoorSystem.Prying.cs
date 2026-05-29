using Content.Shared.Prying.Components;

namespace Content.Shared.Doors.Systems;

public abstract partial class SharedDoorSystem
{
    private partial void RaiseSunrisePriedDoorEvent(EntityUid user, EntityUid door, bool opened)
    {
        var userEvent = new UserPriedDoorEvent(door, opened);
        RaiseLocalEvent(user, ref userEvent);
    }
}
