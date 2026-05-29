#pragma warning disable IDE0130
using Content.Shared.Interaction.Events;

namespace Content.Server.Guardian;

public sealed partial class GuardianSystem
{
    private partial bool CanAttemptGuardianAttack(EntityUid uid, GuardianComponent component, AttackAttemptEvent args)
    {
        if (component.GuardianLoose)
            return true;

        args.Cancel();
        return false;
    }
}
