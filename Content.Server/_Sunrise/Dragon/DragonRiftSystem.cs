using Content.Shared.Sprite;
namespace Content.Server.Dragon;

using Content.Server.Destructible;
using Content.Server.NPC;
using Content.Shared.Damage.Systems;
using Robust.Shared.Map;
using System.Numerics;
using Content.Server._Sunrise.DragonsBrood;

public partial class DragonRiftSystem
{
    private void OnDragonsBroodDead(DragonsBroodDeadEvent args)
    {
        if (!TryComp<DragonRiftComponent>(args.Brood.Comp.MotherRift, out var comp))
            return;

        var entProtoName = Prototype(args.Brood.Owner)?.ID;

        if (entProtoName is null)
            return;

        if (entProtoName == "MobCarp")
        {
            if (comp.AliveCarps > 0)
                comp.AliveCarps--;
            CheckMaxCarpSpawn(comp);
        }

        if (entProtoName == "MobSharkDragon")
        {
            if (comp.AliveSharks > 0)
                comp.AliveSharks--;
            CheckMaxSharkSpawn(comp);
        }
    }

    private void CheckMaxCarpSpawn(DragonRiftComponent comp) =>
        comp.IsCarpSpawnAccumulating = comp.AliveCarps < comp.MaxAliveCarps;

    private void CheckMaxSharkSpawn(DragonRiftComponent comp) =>
        comp.IsSharkSpawnAccumulating = comp.AliveSharks < comp.MaxAliveSharks;

    private void OnDamageChanged(Entity<DragonRiftComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || ent.Comp.SpawnedSharkAtLowHealth)
            return;

        if (
            !TryComp<DestructibleComponent>(ent, out var destructible)
            || !_destructible.TryGetDestroyedAt((ent, destructible), out var destroyedAt)
        )
            return;

        var remainingHealth = (destroyedAt.Value - args.Damageable.TotalDamage).Float();
        if (remainingHealth > ent.Comp.SharkSpawnLowHealthThreshold)
            return;

        ent.Comp.SpawnedSharkAtLowHealth = true;
        SpawnShark(ent.Owner, ent.Comp, Transform(ent));
    }

    private void TrySpawnChargeShark(EntityUid uid, DragonRiftComponent comp, TransformComponent xform, float chargeThreshold, ref bool spawned)
    {
        if (spawned || comp.MaxAccumulator <= 0f || comp.Accumulator < comp.MaxAccumulator * chargeThreshold)
            return;

        spawned = true;
        SpawnShark(uid, comp, xform);
    }

    private void EnsureFinishedSharkSpawn(EntityUid uid, DragonRiftComponent comp, TransformComponent xform)
    {
        if (comp.SpawnedSharkAtFullCharge)
            return;

        comp.SpawnedSharkAtFullCharge = true;
        SpawnShark(uid, comp, xform);
    }

    private void TrySpawnPeriodicFinishedSharks(EntityUid uid, DragonRiftComponent comp, TransformComponent xform, float frameTime)
    {
        if (comp.SharkSpawnCooldown <= 0f)
            return;

        if (comp.IsSharkSpawnAccumulating)
            comp.SharkSpawnAccumulator += frameTime;

        while (
            comp.SharkSpawnAccumulator >= comp.SharkSpawnCooldown
            && comp.AliveSharks < comp.MaxAliveSharks
        )
        {
            comp.SharkSpawnAccumulator -= comp.SharkSpawnCooldown;
            SpawnShark(uid, comp, xform);
        }
    }

    // Does not check for max alive sharks, but increases the count
    private void SpawnShark(EntityUid uid, DragonRiftComponent comp, TransformComponent xform)
    {
        var shark = Spawn(comp.SharkSpawnPrototype, xform.Coordinates);
        comp.AliveSharks++;

        // Updating look to match the dragon
        if (TryComp<RandomSpriteComponent>(comp.Dragon, out var randomSprite))
        {
            var spawnedSprite = EnsureComp<RandomSpriteComponent>(shark);
            _serManager.CopyTo(randomSprite, ref spawnedSprite, notNullableOverride: true);
            Dirty(shark, spawnedSprite);
        }

        if (comp.Dragon is not null)
            _npc.SetBlackboard(shark, NPCBlackboard.FollowTarget, new EntityCoordinates(comp.Dragon.Value, Vector2.Zero));
    }
}
