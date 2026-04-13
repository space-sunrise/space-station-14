using Content.Shared._Sunrise.Weapons.DualWield;
using Content.Shared.Weapons.Ranged.Components;

// Required: partial class must keep the vanilla SharedGunSystem namespace despite the _Sunrise file path.
#pragma warning disable IDE0130
namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    private void StopDualWieldGun(EntityUid? gunUid)
    {
        if (gunUid is not { } uid || !TryComp<GunComponent>(uid, out var gun))
            return;

        StopShooting(uid, gun);
    }
}
