using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._Starlight.Weapons.DualWield;
using Content.Shared.Damage.Components;
using Content.Shared.Hands;
using Content.Shared.Input;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Weapons;

public sealed class WeaponTests : InteractionTest
{
    // MobHuman is required here because the dual-wield test needs separate left and right hands.
    protected override string PlayerPrototype => "MobHuman";
    private static readonly EntProtoId MobHuman = "MobHuman";
    private static readonly EntProtoId SniperMosin = "WeaponSniperMosin";
    private static readonly EntProtoId WeaponPistolTec9 = "WeaponPistolTec9";

    [Test]
    public async Task GunRequiresWieldTest()
    {
        var gunSystem = SEntMan.System<SharedGunSystem>();

        await AddAtmosphere(); // prevent the Urist from suffocating

        var urist = await SpawnTarget(MobHuman);
        var damageComp = Comp<DamageableComponent>(urist);

        var mosinNet = await PlaceInHands(SniperMosin);
        var mosinEnt = ToServer(mosinNet);

        await Pair.RunSeconds(2f); // Guns have a cooldown when picking them up.

        Assert.That(HasComp<GunRequiresWieldComponent>(mosinNet),
            "Looks like you've removed the 'GunRequiresWield' component from the mosin sniper." +
            "If this was intentional, please update WeaponTests.cs to reflect this change!");

        var startAmmo = gunSystem.GetAmmoCount(mosinEnt);
        var wieldComp = Comp<WieldableComponent>(mosinNet);

        Assert.That(startAmmo, Is.GreaterThan(0), "Mosin was spawned with no ammo!");
        Assert.That(wieldComp.Wielded, Is.False, "Mosin was spawned wielded!");

        await AttemptShoot(urist, false); // should fail due to not being wielded
        var updatedAmmo = gunSystem.GetAmmoCount(mosinEnt);

        Assert.That(updatedAmmo,
            Is.EqualTo(startAmmo),
            "Mosin discharged ammo when the weapon should not have fired!");
        Assert.That(damageComp.TotalDamage.Value,
            Is.EqualTo(0),
            "Urist took damage when the weapon should not have fired!");

        await UseInHand();

        Assert.That(wieldComp.Wielded, Is.True, "Mosin failed to wield when interacted with!");

        await AttemptShoot(urist);
        updatedAmmo = gunSystem.GetAmmoCount(mosinEnt);
        // Sunrise-start
        Assert.That(updatedAmmo, Is.EqualTo(startAmmo), "Mosin should keep ammo count until the bolt is cycled!");

        await PressKey(ContentKeyFunctions.CockGun);
        updatedAmmo = gunSystem.GetAmmoCount(mosinEnt);

        Assert.That(updatedAmmo, Is.EqualTo(startAmmo - 1), "Mosin failed to discharge appropriate amount of ammo after cycling!");
        // Sunrise-end
        Assert.That(damageComp.TotalDamage.Value,
            Is.GreaterThan(0),
            "Mosin was fired but urist sustained no damage!");
    }

    // Sunrise edit start - cover dual-wield shoot-stop behavior
    [Test]
    public async Task DualWieldShootStopShootTest()
    {
        await AddAtmosphere();
        var (leftGunNet, rightGunNet) = await EquipDualWieldGuns(WeaponPistolTec9);
        var leftGunUid = ToServer(leftGunNet);
        var rightGunUid = ToServer(rightGunNet);
        var dualWieldSystem = SEntMan.System<SharedDualWieldSystem>();

        await Pair.RunSeconds(2f);

        await Server.WaitPost(() => dualWieldSystem.ToggleDualWield(SPlayer, leftGunUid, rightGunUid, false));
        await RunTicks(5);

        Assert.That(SEntMan.TryGetComponent(SPlayer, out DualWieldComponent? dualWield) && dualWield.Active,
            "Dual wield should become active after equipping two compatible pistols.");

        Assert.Multiple(() =>
        {
            var leftGun = SEntMan.GetComponent<GunComponent>(leftGunUid);
            var rightGun = SEntMan.GetComponent<GunComponent>(rightGunUid);
            var leftDualWield = SEntMan.GetComponent<CanDualWieldComponent>(leftGunUid);
            var rightDualWield = SEntMan.GetComponent<CanDualWieldComponent>(rightGunUid);

            var expectedLeftFireRate = leftDualWield.DualWieldMaxFireRate > 0f
                ? MathF.Min(leftGun.FireRate * leftDualWield.DualWieldFireRateMultiplier, leftDualWield.DualWieldMaxFireRate)
                : leftGun.FireRate * leftDualWield.DualWieldFireRateMultiplier;
            var expectedRightFireRate = rightDualWield.DualWieldMaxFireRate > 0f
                ? MathF.Min(rightGun.FireRate * rightDualWield.DualWieldFireRateMultiplier, rightDualWield.DualWieldMaxFireRate)
                : rightGun.FireRate * rightDualWield.DualWieldFireRateMultiplier;

            Assert.That(leftGun.FireRateModified, Is.EqualTo(expectedLeftFireRate).Within(0.001f));
            Assert.That(rightGun.FireRateModified, Is.EqualTo(expectedRightFireRate).Within(0.001f));
        });

        var leftAmmo = SGun.GetAmmoCount(leftGunUid);
        var rightAmmo = SGun.GetAmmoCount(rightGunUid);
        var firstGunUid = dualWield.NextIsLeft ? leftGunUid : rightGunUid;
        var secondGunUid = dualWield.NextIsLeft ? rightGunUid : leftGunUid;
        var firstGunNet = dualWield.NextIsLeft ? leftGunNet : rightGunNet;
        var secondGunNet = dualWield.NextIsLeft ? rightGunNet : leftGunNet;
        var firstGunStartAmmo = firstGunUid == leftGunUid ? leftAmmo : rightAmmo;
        var secondGunStartAmmo = secondGunUid == leftGunUid ? leftAmmo : rightAmmo;
        var expectedFirstAmmoAfterFirstShot = firstGunStartAmmo - 1;
        var expectedSecondAmmoAfterFirstShot = secondGunStartAmmo;
        var expectedFirstAmmoAfterSecondShot = firstGunStartAmmo - 1;
        var expectedSecondAmmoAfterSecondShot = secondGunStartAmmo - 1;

        await SetCombatMode(true);

        await ShootFromClient(firstGunNet);

        Assert.Multiple(() =>
        {
            Assert.That(SGun.GetAmmoCount(firstGunUid), Is.EqualTo(expectedFirstAmmoAfterFirstShot));
            Assert.That(SGun.GetAmmoCount(secondGunUid), Is.EqualTo(expectedSecondAmmoAfterFirstShot));
            Assert.That(SEntMan.GetComponent<GunComponent>(firstGunUid).ShotCounter, Is.EqualTo(1));
            Assert.That(SEntMan.GetComponent<GunComponent>(secondGunUid).ShotCounter, Is.EqualTo(0));
        });

        await StopShootingFromClient(firstGunNet);

        Assert.Multiple(() =>
        {
            Assert.That(SEntMan.GetComponent<GunComponent>(leftGunUid).ShotCounter, Is.EqualTo(0));
            Assert.That(SEntMan.GetComponent<GunComponent>(rightGunUid).ShotCounter, Is.EqualTo(0));
        });

        await ShootFromClient(secondGunNet);

        Assert.Multiple(() =>
        {
            Assert.That(SGun.GetAmmoCount(firstGunUid), Is.EqualTo(expectedFirstAmmoAfterSecondShot));
            Assert.That(SGun.GetAmmoCount(secondGunUid), Is.EqualTo(expectedSecondAmmoAfterSecondShot));
            Assert.That(SEntMan.GetComponent<GunComponent>(firstGunUid).ShotCounter, Is.EqualTo(0));
            Assert.That(SEntMan.GetComponent<GunComponent>(secondGunUid).ShotCounter, Is.EqualTo(1));
        });
    }

    /// <summary>
    ///     Spawns and equips two guns of the specified prototype into the player's left and right hands.
    ///     Asserts that the test player has separate left and right hands before equipping them.
    /// </summary>
    /// <param name="prototype">The gun prototype to spawn in both hands.</param>
    /// <returns>The network entities of the left-hand and right-hand guns.</returns>
    private async Task<(NetEntity LeftGun, NetEntity RightGun)> EquipDualWieldGuns(EntProtoId prototype)
    {
        NetEntity leftGun = default;
        NetEntity rightGun = default;

        await Server.WaitPost(() =>
        {
            Assert.That(Hands, Is.Not.Null);

            string? leftHand = null;
            string? rightHand = null;

            foreach (var handId in Hands!.SortedHands)
            {
                if (!HandSys.TryGetHand((SPlayer, Hands), handId, out var hand))
                    continue;

                switch (hand.Value.Location)
                {
                    case HandLocation.Left:
                        leftHand = handId;
                        break;
                    case HandLocation.Right:
                        rightHand = handId;
                        break;
                }
            }

            Assert.That(leftHand, Is.Not.Null.And.Not.EqualTo(rightHand), "Player should have separate left and right hands.");

            var leftEntity = SEntMan.SpawnEntity(prototype, SEntMan.GetCoordinates(PlayerCoords));
            var rightEntity = SEntMan.SpawnEntity(prototype, SEntMan.GetCoordinates(PlayerCoords));

            Assert.That(HandSys.TryPickup(SPlayer, leftEntity, leftHand, false, false, false, Hands));
            Assert.That(HandSys.TryPickup(SPlayer, rightEntity, rightHand, false, false, false, Hands));

            if (Hands.ActiveHandId != leftHand)
                Assert.That(HandSys.TrySetActiveHand((SPlayer, Hands), leftHand));

            leftGun = SEntMan.GetNetEntity(leftEntity);
            rightGun = SEntMan.GetNetEntity(rightEntity);
        });

        await RunTicks(5);
        return (leftGun, rightGun);
    }

    /// <summary>
    ///     Raises a client-side shoot request for the specified gun.
    /// </summary>
    private async Task ShootFromClient(NetEntity gun)
    {
        await Client.WaitPost(() => CEntMan.RaisePredictiveEvent(new RequestShootEvent
        {
            Gun = gun,
            Coordinates = TargetCoords,
        }));

        await RunTicks(5);
    }

    /// <summary>
    ///     Raises a client-side stop shooting request for the specified gun.
    /// </summary>
    private async Task StopShootingFromClient(NetEntity gun)
    {
        await Client.WaitPost(() => CEntMan.RaisePredictiveEvent(new RequestStopShootEvent
        {
            Gun = gun,
        }));

        await RunTicks(5);
    }
    // Sunrise edit end
}
