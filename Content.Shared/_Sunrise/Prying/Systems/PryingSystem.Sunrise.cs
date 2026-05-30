using Content.Shared.Prying.Components;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Prying.Systems;

public sealed partial class PryingSystem
{
    private partial bool CanSunrisePry(EntityUid target, EntityUid user, ref BeforePryEvent pryEvent, out string? message)
        => (message = null) is null;

    private partial bool TrySunrisePry(EntityUid target, EntityUid user, BeforePryEvent pryEvent, out string? message)
    {
        var userEvent = new UserBeforePryEvent(target, pryEvent.PryPowered, pryEvent.Force, pryEvent.StrongPry);
        RaiseLocalEvent(user, ref userEvent);

        (message, var cancelled) = (userEvent.Message, userEvent.Cancelled);
        return !cancelled;
    }
}
