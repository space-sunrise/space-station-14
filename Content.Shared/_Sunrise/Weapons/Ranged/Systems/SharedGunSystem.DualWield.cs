using Content.Shared._Sunrise.Weapons.DualWield;
using Content.Shared.Weapons.Ranged.Components;

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
