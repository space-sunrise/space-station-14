using System.Numerics;
using System.Threading.Tasks;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared._Sunrise.Weapons.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using NUnit.Framework;

namespace Content.IntegrationTests.Tests._Sunrise.Weapons;

[TestFixture]
public sealed class SunriseWeaponTests : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    // Targets
    private static readonly string MobHumanProto = "MobHuman";
    private static readonly string WoodDoorProto = "WoodDoor";
    private static readonly string WallSolidProto = "WallSolid";

    // Armor
    private static readonly string ArmorHeavy = "ClothingOuterArmorHeavy";

    [TestPrototypes]
    private const string TestPrototypes = @"
- type: entity
  id: BulletTestFMJ
  parent: BulletPistolTraceFMJ
  components:
  - type: ProjectilePierce
    chance: 1.0
  - type: ProjectileRicochet
    chance: 1.0

- type: entity
  id: BulletTestHP
  parent: BulletPistolTraceHP
  components:
  - type: Projectile
    damage:
      types:
        Piercing: 20

- type: entity
  id: BulletTestAP
  parent: BulletPistolTraceAP
  components:
  - type: Projectile
    armorPenetration: 0.8
    damage:
      types:
        Piercing: 15
";

    [Test]
    public async Task TestRicochetAngle()
    {
        // Spawns a shuttle wall (with 100% ricochet chance) to bounce a bullet off
        var wall = await SpawnTarget("WallShuttle", TargetCoords);

        // Spawn a bullet moving down-right (shallow angle) towards the top face of the wall
        // Position: (X = 1.5, Y = 1.2) - directly above the wall
        // To hit exactly the center of the wall at (0, 0.5) with velocity (10f, -3f),
        // we offset the spawn X by -0.67f relative to the wall center (0, 0.7)
        var bulletCoords = new EntityCoordinates(SEntMan.GetEntity(wall), new Vector2(-0.67f, 0.7f));

        EntityUid bullet = default!;
        await Server.WaitPost(() =>
        {
            bullet = SEntMan.SpawnEntity("BulletTestFMJ", bulletCoords);

            var proj = SEntMan.GetComponent<ProjectileComponent>(bullet);
            proj.OnlyCollideWhenShot = false;
            SEntMan.System<SharedProjectileSystem>().SetShooter(bullet, proj, SPlayer);

            var physics = SEntMan.GetComponent<PhysicsComponent>(bullet);

            // Set speed and direction pointing at a shallow angle to guarantee 100% ricochet probability
            var dir = new Vector2(10f, -3f);
            SEntMan.System<SharedPhysicsSystem>().SetLinearVelocity(bullet, dir, body: physics);
            SEntMan.System<SharedTransformSystem>().SetWorldRotation(bullet, dir.ToWorldAngle() + proj.Angle);
        });

        // Run ticks for physics simulation to collide and bounce
        await RunTicks(4);

        // Assert bullet is still alive and ricocheted upwards
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(bullet), "Bullet was deleted instead of ricocheting!");
            var physics = SEntMan.GetComponent<PhysicsComponent>(bullet);
            var velocity = physics.LinearVelocity;

            // The reflected direction should be upwards (Y > 0)
            Assert.That(velocity.Y, Is.GreaterThan(0f), $"Expected reflection Y velocity to be positive (upward), but got: {velocity.Y}");
            Assert.That(velocity.X, Is.GreaterThan(0f), $"Expected X velocity to be positive, but got: {velocity.X}");
        });

        // Cleanup wall and bullet
        await Delete(wall);
        if (SEntMan.EntityExists(bullet))
            await Delete(bullet);
    }

    [Test]
    public async Task TestPierceHumanAndDoor()
    {
        // Add atmosphere so human doesn't choke
        await AddAtmosphere();

        // 1. Spawns a naked human to pierce
        var targetCoords = SEntMan.GetCoordinates(PlayerCoords).Offset(new Vector2(2.5f, 0f));
        var targetNetCoords = SEntMan.GetNetCoordinates(targetCoords);
        var human = await SpawnTarget(MobHumanProto, targetNetCoords);
        var damageComp = Comp<DamageableComponent>(human);

        // Spawn a bullet flying straight through the human, spawned well outside the player's 0.35m collision radius (at 1.2m)
        var bulletCoords = SEntMan.GetCoordinates(PlayerCoords).Offset(new Vector2(1.2f, 0f));

        EntityUid bullet = default!;
        await Server.WaitPost(() =>
        {
            bullet = SEntMan.SpawnEntity("BulletTestFMJ", bulletCoords);

            var proj = SEntMan.GetComponent<ProjectileComponent>(bullet);
            proj.OnlyCollideWhenShot = false;
            SEntMan.System<SharedProjectileSystem>().SetShooter(bullet, proj, SPlayer);

            var physics = SEntMan.GetComponent<PhysicsComponent>(bullet);
            var dir = new Vector2(20f, 0f); // Fast velocity straight right
            SEntMan.System<SharedPhysicsSystem>().SetLinearVelocity(bullet, dir, body: physics);
            SEntMan.System<SharedTransformSystem>().SetWorldRotation(bullet, dir.ToWorldAngle() + proj.Angle);
        });

        // Run ticks for the collision and piercing to complete
        for (int i = 0; i < 5; i++)
        {
            await RunTicks(1);
            await Server.WaitAssertion(() =>
            {
                if (SEntMan.EntityExists(bullet))
                {
                    var xform = SEntMan.GetComponent<TransformComponent>(bullet);
                    var physics = SEntMan.GetComponent<PhysicsComponent>(bullet);
                    NUnit.Framework.TestContext.Out.WriteLine($"[TICK {i+1}] Pos: {xform.LocalPosition}, Vel: {physics.LinearVelocity}");
                }
                else
                {
                    NUnit.Framework.TestContext.Out.WriteLine($"[TICK {i+1}] Bullet DELETED");
                }
            });
        }

        await Server.WaitAssertion(() =>
        {
            Assert.That(damageComp.TotalDamage.Value, Is.GreaterThan(0), "Human took no damage!");
            Assert.That(SEntMan.EntityExists(bullet), "Bullet was deleted instead of piercing human!");

            var physics = SEntMan.GetComponent<PhysicsComponent>(bullet);
            Assert.That(physics.LinearVelocity.X, Is.GreaterThan(10f), "Bullet lost too much forward velocity or reversed!");
        });

        // Delete bullet and human before door test
        await Delete(human);
        if (SEntMan.EntityExists(bullet))
            await Delete(bullet);

        // 2. Spawns a wooden door to pierce
        var door = await SpawnTarget(WoodDoorProto, targetNetCoords);

        await Server.WaitPost(() =>
        {
            bullet = SEntMan.SpawnEntity("BulletTestFMJ", bulletCoords);

            var proj = SEntMan.GetComponent<ProjectileComponent>(bullet);
            proj.OnlyCollideWhenShot = false;
            SEntMan.System<SharedProjectileSystem>().SetShooter(bullet, proj, SPlayer);

            var physics = SEntMan.GetComponent<PhysicsComponent>(bullet);
            var dir = new Vector2(20f, 0f);
            SEntMan.System<SharedPhysicsSystem>().SetLinearVelocity(bullet, dir, body: physics);
            SEntMan.System<SharedTransformSystem>().SetWorldRotation(bullet, dir.ToWorldAngle() + proj.Angle);
        });

        await RunTicks(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(bullet), "Bullet was deleted instead of piercing WoodDoor!");
        });

        // Cleanup door and bullet
        await Delete(door);
        if (SEntMan.EntityExists(bullet))
            await Delete(bullet);
    }

    [Test]
    public async Task TestAmmunitionDamageByArmor()
    {
        await AddAtmosphere();

        var targetCoords = SEntMan.GetCoordinates(PlayerCoords).Offset(new Vector2(2.5f, 0f));
        var targetNetCoords = SEntMan.GetNetCoordinates(targetCoords);
        var bulletCoords = SEntMan.GetCoordinates(PlayerCoords).Offset(new Vector2(1.2f, 0f));

        // 1. HP Bullet vs Unarmored target (should deal extremely high damage)
        var human1 = await SpawnTarget(MobHumanProto, targetNetCoords);
        var damageComp1 = Comp<DamageableComponent>(human1);

        EntityUid bullet = default!;
        await Server.WaitPost(() =>
        {
            bullet = SEntMan.SpawnEntity("BulletTestHP", bulletCoords);
            var proj = SEntMan.GetComponent<ProjectileComponent>(bullet);
            proj.OnlyCollideWhenShot = false;
            SEntMan.System<SharedProjectileSystem>().SetShooter(bullet, proj, SPlayer);

            var physics = SEntMan.GetComponent<PhysicsComponent>(bullet);
            var dir = new Vector2(20f, 0f);
            SEntMan.System<SharedPhysicsSystem>().SetLinearVelocity(bullet, dir, body: physics);
            SEntMan.System<SharedTransformSystem>().SetWorldRotation(bullet, dir.ToWorldAngle() + proj.Angle);
        });
        await RunTicks(5);
        var hpUnarmoredDamage = damageComp1.TotalDamage.Value;

        // Clean up human1 and bullet
        await Delete(human1);
        if (SEntMan.EntityExists(bullet))
            await Delete(bullet);

        // 2. AP Bullet vs Armored target (should penetrate well and deal high damage)
        var human2 = await SpawnTarget(MobHumanProto, targetNetCoords);
        var damageComp2 = Comp<DamageableComponent>(human2);

        // Equip heavy armor on human2
        var inventorySystem = SEntMan.System<Content.Shared.Inventory.InventorySystem>();
        var armor = default(EntityUid);
        await Server.WaitPost(() =>
        {
            armor = SEntMan.SpawnEntity(ArmorHeavy, targetCoords);
            inventorySystem.TryUnequip(ToServer(human2), "outerClothing", force: true);
            var equipped = inventorySystem.TryEquip(ToServer(human2), armor, "outerClothing", force: true);
            Assert.That(equipped, Is.True, "Failed to equip heavy armor on human2!");
        });

        await Server.WaitPost(() =>
        {
            bullet = SEntMan.SpawnEntity("BulletTestAP", bulletCoords);
            var proj = SEntMan.GetComponent<ProjectileComponent>(bullet);
            proj.OnlyCollideWhenShot = false;
            SEntMan.System<SharedProjectileSystem>().SetShooter(bullet, proj, SPlayer);

            var physics = SEntMan.GetComponent<PhysicsComponent>(bullet);
            var dir = new Vector2(20f, 0f);
            SEntMan.System<SharedPhysicsSystem>().SetLinearVelocity(bullet, dir, body: physics);
            SEntMan.System<SharedTransformSystem>().SetWorldRotation(bullet, dir.ToWorldAngle() + proj.Angle);
        });
        await RunTicks(5);
        var apArmoredDamage = damageComp2.TotalDamage.Value;

        // Clean up human2 and bullet
        await Delete(human2);
        if (SEntMan.EntityExists(bullet))
            await Delete(bullet);

        // 3. HP Bullet vs Armored target (should be heavily mitigated, dealing very low damage)
        var human3 = await SpawnTarget(MobHumanProto, targetNetCoords);
        var damageComp3 = Comp<DamageableComponent>(human3);

        await Server.WaitPost(() =>
        {
            armor = SEntMan.SpawnEntity(ArmorHeavy, targetCoords);
            inventorySystem.TryUnequip(ToServer(human3), "outerClothing", force: true);
            var equipped = inventorySystem.TryEquip(ToServer(human3), armor, "outerClothing", force: true);
            Assert.That(equipped, Is.True, "Failed to equip heavy armor on human3!");
        });

        await Server.WaitPost(() =>
        {
            bullet = SEntMan.SpawnEntity("BulletTestHP", bulletCoords);
            var proj = SEntMan.GetComponent<ProjectileComponent>(bullet);
            proj.OnlyCollideWhenShot = false;
            SEntMan.System<SharedProjectileSystem>().SetShooter(bullet, proj, SPlayer);

            var physics = SEntMan.GetComponent<PhysicsComponent>(bullet);
            var dir = new Vector2(20f, 0f);
            SEntMan.System<SharedPhysicsSystem>().SetLinearVelocity(bullet, dir, body: physics);
            SEntMan.System<SharedTransformSystem>().SetWorldRotation(bullet, dir.ToWorldAngle() + proj.Angle);
        });
        await RunTicks(5);
        var hpArmoredDamage = damageComp3.TotalDamage.Value;

        // Clean up human3 and bullet
        await Delete(human3);
        if (SEntMan.EntityExists(bullet))
            await Delete(bullet);

        // Asserts to verify damage profiles
        Assert.Multiple(() =>
        {
            Assert.That(hpUnarmoredDamage, Is.GreaterThan(hpArmoredDamage),
                $"HP bullet should deal way more damage to unarmored targets ({hpUnarmoredDamage}) than armored targets ({hpArmoredDamage})!");
            Assert.That(apArmoredDamage, Is.GreaterThan(hpArmoredDamage),
                $"AP bullet should penetrate armored targets better ({apArmoredDamage}) than HP bullet ({hpArmoredDamage})!");
        });
    }

    [Test]
    public async Task TestHPAmmoDoesNotHealArmored()
    {
        await AddAtmosphere();

        var targetCoords = SEntMan.GetCoordinates(PlayerCoords).Offset(new Vector2(2.5f, 0f));
        var targetNetCoords = SEntMan.GetNetCoordinates(targetCoords);
        var bulletCoords = SEntMan.GetCoordinates(PlayerCoords).Offset(new Vector2(1.2f, 0f));

        var human = await SpawnTarget(MobHumanProto, targetNetCoords);
        var damageComp = Comp<DamageableComponent>(human);

        // 1. Deal some initial damage to the target
        var initialDamage = new DamageSpecifier();
        var piercingProto = ProtoMan.Index<DamageTypePrototype>("Piercing");
        initialDamage.DamageDict.Add(piercingProto.ID, 50);

        await Server.WaitPost(() =>
        {
            var damageableSystem = SEntMan.System<DamageableSystem>();
            damageableSystem.TryChangeDamage(ToServer(human), initialDamage, ignoreResistances: true, ignoreGlobalModifiers: true, ignoreVariance: true);
        });

        await Server.WaitAssertion(() =>
        {
            Assert.That(damageComp.TotalDamage, Is.EqualTo(FixedPoint2.New(50)), "Target did not receive initial damage!");
        });

        // 2. Equip heavy armor on the target
        var inventorySystem = SEntMan.System<Content.Shared.Inventory.InventorySystem>();
        var armor = default(EntityUid);
        await Server.WaitPost(() =>
        {
            armor = SEntMan.SpawnEntity(ArmorHeavy, targetCoords);
            inventorySystem.TryUnequip(ToServer(human), "outerClothing", force: true);
            var equipped = inventorySystem.TryEquip(ToServer(human), armor, "outerClothing", force: true);
            Assert.That(equipped, Is.True, "Failed to equip heavy armor!");
        });

        // 3. Shoot the target with a BulletTestHP (which has negative armor penetration -1.0)
        EntityUid bullet = default!;
        await Server.WaitPost(() =>
        {
            bullet = SEntMan.SpawnEntity("BulletTestHP", bulletCoords);
            var proj = SEntMan.GetComponent<ProjectileComponent>(bullet);
            proj.OnlyCollideWhenShot = false;
            SEntMan.System<SharedProjectileSystem>().SetShooter(bullet, proj, SPlayer);

            var physics = SEntMan.GetComponent<PhysicsComponent>(bullet);
            var dir = new Vector2(20f, 0f);
            SEntMan.System<SharedPhysicsSystem>().SetLinearVelocity(bullet, dir, body: physics);
            SEntMan.System<SharedTransformSystem>().SetWorldRotation(bullet, dir.ToWorldAngle() + proj.Angle);
        });
        await RunTicks(5);

        // 4. Assert that the target has NOT been healed (i.e. damage did not decrease below 50)
        await Server.WaitAssertion(() =>
        {
            Assert.That(damageComp.TotalDamage, Is.GreaterThanOrEqualTo(FixedPoint2.New(50)),
                $"HP bullet healed the armored target! Expected at least 50 damage, but got: {damageComp.TotalDamage}");
        });

        // Cleanup
        await Delete(human);
        if (SEntMan.EntityExists(bullet))
            await Delete(bullet);
    }
}
