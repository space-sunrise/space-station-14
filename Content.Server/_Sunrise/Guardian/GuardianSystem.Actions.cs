#pragma warning disable IDE0130 // Namespace does not match folder structure
using Content.Shared.Actions;

namespace Content.Server.Guardian;

public sealed partial class GuardianSystem
{
    partial void OnGuardianLooseChanged(EntityUid guardian, GuardianComponent guardianComponent)
    {
        if (!TryComp<ActionGrantComponent>(guardian, out var actionGrant))
            return;

        foreach (var action in actionGrant.ActionEntities)
            _actionSystem.SetEnabled(action, guardianComponent.GuardianLoose);
    }
}
