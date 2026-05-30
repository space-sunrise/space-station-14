#pragma warning disable IDE0130
using Content.Shared.Interaction.Events;

namespace Content.Server.Guardian;

public sealed partial class GuardianSystem
{
    /// <summary>
    /// Checks whether a guardian may attempt an attack. Cancels <paramref name="args"/> as a side-effect when returning false,
    /// since the partial implementation is the only place permitted to mutate the event without touching the upstream file.
    /// </summary>
    private partial bool CanAttemptGuardianAttack(EntityUid uid, GuardianComponent component, AttackAttemptEvent args)
    {
        if (component.GuardianLoose)
            return true;

        args.Cancel();
        return false;
    }
}
