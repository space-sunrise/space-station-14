#pragma warning disable IDE0130
using Content.Shared._Sunrise.Guardian;

namespace Content.Server.Guardian;

public sealed partial class GuardianSystem
{
    private partial bool CanAttackHost(EntityUid uid)
    {
        return HasComp<GuardianAllowHostMeleeComponent>(uid);
    }
}
