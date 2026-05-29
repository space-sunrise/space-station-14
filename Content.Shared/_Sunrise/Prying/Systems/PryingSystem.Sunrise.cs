using Content.Shared.Prying.Components;

namespace Content.Shared.Prying.Systems;

public sealed partial class PryingSystem
{
    private partial bool CanSunrisePry(EntityUid target, EntityUid user, ref BeforePryEvent pryEvent, out string? message)
    {
        var userEvent = new UserBeforePryEvent(target, pryEvent.PryPowered, pryEvent.Force, pryEvent.StrongPry);
        RaiseLocalEvent(user, ref userEvent);

        (message, var cancelled) = (userEvent.Message, userEvent.Cancelled);
        return !cancelled;
    }
}
