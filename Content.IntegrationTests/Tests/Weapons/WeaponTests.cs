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

    // Sunrise-start
    [Test]
    public async Task DualWieldShootStopShootTest()
    {
        await AddAtmosphere();
        var (leftGunNet, rightGunNet) = await EquipDualWieldGuns(WeaponPistolTec9);
        var leftGunUid = ToServer(leftGunNet);
        var rightGunUid = ToServer(rightGunNet);

        await Pair.RunSeconds(2f);

        Assert.That(TryComp<DualWieldComponent>(SPlayer, out var dualWield) && dualWield.Active,
            "Dual wield should become active after equipping two compatible pistols.");

        Assert.Multiple(() =>
        {
            var leftGun = Comp<GunComponent>(leftGunUid);
            var rightGun = Comp<GunComponent>(rightGunUid);
            Assert.That(leftGun.FireRateModified, Is.EqualTo(leftGun.FireRate).Within(0.001f));
            Assert.That(rightGun.FireRateModified, Is.EqualTo(rightGun.FireRate).Within(0.001f));
        });

        var leftAmmo = SGun.GetAmmoCount(leftGunUid);
        var rightAmmo = SGun.GetAmmoCount(rightGunUid);

        await SetCombatMode(true);

        await ShootFromClient(leftGunNet);

        Assert.Multiple(() =>
        {
            Assert.That(SGun.GetAmmoCount(leftGunUid), Is.EqualTo(leftAmmo - 1));
            Assert.That(SGun.GetAmmoCount(rightGunUid), Is.EqualTo(rightAmmo - 1));
            Assert.That(Comp<GunComponent>(leftGunUid).ShotCounter, Is.EqualTo(1));
            Assert.That(Comp<GunComponent>(rightGunUid).ShotCounter, Is.EqualTo(1));
        });

        await StopShootingFromClient(leftGunNet);

        Assert.Multiple(() =>
        {
            Assert.That(Comp<GunComponent>(leftGunUid).ShotCounter, Is.EqualTo(0));
            Assert.That(Comp<GunComponent>(rightGunUid).ShotCounter, Is.EqualTo(0));
        });

        await ShootFromClient(leftGunNet);

        Assert.Multiple(() =>
        {
            Assert.That(SGun.GetAmmoCount(leftGunUid), Is.EqualTo(leftAmmo - 2));
            Assert.That(SGun.GetAmmoCount(rightGunUid), Is.EqualTo(rightAmmo - 2));
        });
    }

    /// <summary>
    ///     Spawns and equips two guns of the specified prototype into the player's left and right hands.
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

                switch (hand.Location)
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
            Assert.That(rightHand, Is.Not.Null, "Player should have a right hand.");

            var leftEntity = SEntMan.SpawnEntity(prototype, SEntMan.GetCoordinates(PlayerCoords));
            var rightEntity = SEntMan.SpawnEntity(prototype, SEntMan.GetCoordinates(PlayerCoords));

            Assert.That(HandSys.TryPickup(SPlayer, leftEntity, leftHand, false, false, false, Hands));
            Assert.That(HandSys.TryPickup(SPlayer, rightEntity, rightHand, false, false, false, Hands));

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
    // Sunrise-end
}
