using Content.Server.Chat.Systems;
using Content.Server.Destructible; // Sunrise-edit
using Content.Server.NPC;
using Content.Server.NPC.Systems;
using Content.Server.Pinpointer;
using Content.Shared.Damage.Systems; // Sunrise-edit
using Content.Shared.Dragon;
using Content.Shared.Examine;
using Content.Shared.Sprite;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager;
using System.Numerics;
using Content.Shared.Damage.Components;
using Robust.Shared.Audio; // Sunrise-edit
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
// Sunrise-Start
using Content.Shared.Mobs.Components;
using Content.Server._Sunrise.DragonsBrood;
// Sunrise-End

namespace Content.Server.Dragon;

/// <summary>
/// Handles events for rift entities and rift updating.
/// </summary>
public sealed class DragonRiftSystem : EntitySystem
{
    private static readonly SoundSpecifier RiftWarningSound = new SoundPathSpecifier("/Audio/Misc/notice1.ogg"); // Sunrise-edit

    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DragonSystem _dragon = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!; // Sunrise-edit
    [Dependency] private readonly ISerializationManager _serManager = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DragonRiftComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<DragonRiftComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DragonRiftComponent, AnchorStateChangedEvent>(OnAnchorChange);
        SubscribeLocalEvent<DragonRiftComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DragonRiftComponent, DamageChangedEvent>(OnDamageChanged); // Sunrise-edit
        SubscribeLocalEvent<DragonsBroodDeadEvent>(OnDragonsBroodDead); // Sunrise-Add
    }

    private void OnGetState(Entity<DragonRiftComponent> ent, ref ComponentGetState args)
    {
        args.State = new DragonRiftComponentState
        {
            State = ent.Comp.State,
        };
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DragonRiftComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.State != DragonRiftState.Finished && comp.Accumulator >= comp.MaxAccumulator)
            {
                // TODO: When we get autocall you can buff if the rift finishes / 3 rifts are up
                // for now they just keep 3 rifts up.

                if (comp.Dragon != null)
                    _dragon.RiftCharged(comp.Dragon.Value);

                comp.Accumulator = comp.MaxAccumulator;
                RemComp<DamageableComponent>(uid);
                comp.State = DragonRiftState.Finished;
                EnsureFinishedSharkSpawn(uid, comp, xform); // Sunrise-edit
                Dirty(uid, comp);
            }
            else if (comp.State != DragonRiftState.Finished)
            {
                comp.Accumulator += frameTime;
            }

            // Sunrise-Start
            if (comp.IsSpawnAccumulating)
            {
                comp.SpawnAccumulator += frameTime;
            }
            // Sunrise-End

            if (comp.State < DragonRiftState.AlmostFinished && comp.Accumulator > comp.MaxAccumulator / 2f)
            {
                comp.State = DragonRiftState.AlmostFinished;
                Dirty(uid, comp);

                var msg = Loc.GetString("carp-rift-warning",
                    ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((uid, xform)))));
                _chat.DispatchGlobalAnnouncement(msg, playDefault: false, colorOverride: Color.Red);
                _audio.PlayGlobal(RiftWarningSound, Filter.Broadcast(), true); // Sunrise-edit
                _navMap.SetBeaconEnabled(uid, true);
            }
            // Sunrise-start
            if (comp.State != DragonRiftState.Finished)
            {
                TrySpawnChargeShark(uid, comp, xform, 0.5f, ref comp.SpawnedSharkAtHalfCharge);
                TrySpawnChargeShark(uid, comp, xform, 0.75f, ref comp.SpawnedSharkAtSeventyFiveCharge);
            }
            else
            {
                EnsureFinishedSharkSpawn(uid, comp, xform);
                TrySpawnPeriodicFinishedSharks(uid, comp, xform, frameTime);
            }
            // Sunrise-end
            if (comp.SpawnAccumulator > comp.SpawnCooldown)
            {
                comp.SpawnAccumulator -= comp.SpawnCooldown;
                var ent = Spawn(comp.SpawnPrototype, xform.Coordinates);

                // Update their look to match the leader.
                if (TryComp<RandomSpriteComponent>(comp.Dragon, out var randomSprite))
                {
                    var spawnedSprite = EnsureComp<RandomSpriteComponent>(ent);
                    _serManager.CopyTo(randomSprite, ref spawnedSprite, notNullableOverride: true);
                    Dirty(ent, spawnedSprite);
                }

                if (comp.Dragon != null)
                    _npc.SetBlackboard(ent, NPCBlackboard.FollowTarget, new EntityCoordinates(comp.Dragon.Value, Vector2.Zero));

                // Sunrise-Start
                if (HasComp<MobStateComponent>(ent))
                {
                    AddComp(ent, new DragonsBroodComponent { MotherRift = uid });
                    comp.AliveCarps++;
                    CheckMaxSpawn(comp);
                }

                // Sunrise-End
            }
        }
    }

    // Sunrise-Start
    private void OnDragonsBroodDead(DragonsBroodDeadEvent args)
    {
        if (args.Rift.Comp.AliveCarps > 0)
            args.Rift.Comp.AliveCarps--;

        CheckMaxSpawn(args.Rift.Comp);
    }

    private void CheckMaxSpawn(DragonRiftComponent comp) => comp.IsSpawnAccumulating = comp.AliveCarps < comp.MaxAliveCarps;

    // Sunrise-start
    private void OnDamageChanged(Entity<DragonRiftComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || ent.Comp.SpawnedSharkAtLowHealth)
            return;

        if (!TryComp<DestructibleComponent>(ent, out var destructible) ||
            !_destructible.TryGetDestroyedAt((ent, destructible), out var destroyedAt))
        {
            return;
        }

        var remainingHealth = (destroyedAt.Value - args.Damageable.TotalDamage).Float();
        if (remainingHealth > ent.Comp.SharkLowHealthThreshold)
            return;

        ent.Comp.SpawnedSharkAtLowHealth = true;
        SpawnBonusShark(ent.Owner, ent.Comp, Transform(ent));
    }

    private void TrySpawnChargeShark(EntityUid uid, DragonRiftComponent comp, TransformComponent xform, float chargeThreshold, ref bool spawned)
    {
        if (spawned || comp.MaxAccumulator <= 0f || comp.Accumulator < comp.MaxAccumulator * chargeThreshold)
            return;

        spawned = true;
        SpawnBonusShark(uid, comp, xform);
    }

    private void EnsureFinishedSharkSpawn(EntityUid uid, DragonRiftComponent comp, TransformComponent xform)
    {
        if (comp.SpawnedSharkAtFullCharge)
            return;

        comp.SpawnedSharkAtFullCharge = true;
        SpawnBonusShark(uid, comp, xform);
    }

    private void TrySpawnPeriodicFinishedSharks(EntityUid uid, DragonRiftComponent comp, TransformComponent xform, float frameTime)
    {
        if (comp.SharkSpawnCooldown <= 0f)
            return;

        comp.SharkSpawnAccumulator += frameTime;
        while (comp.SharkSpawnAccumulator >= comp.SharkSpawnCooldown)
        {
            comp.SharkSpawnAccumulator -= comp.SharkSpawnCooldown;
            SpawnBonusShark(uid, comp, xform);
        }
    }

    private void SpawnBonusShark(EntityUid uid, DragonRiftComponent comp, TransformComponent xform)
    {
        var shark = Spawn(comp.SharkSpawnPrototype, xform.Coordinates);

        if (comp.Dragon != null)
            _npc.SetBlackboard(shark, NPCBlackboard.FollowTarget, new EntityCoordinates(comp.Dragon.Value, Vector2.Zero));
    }
    // Sunrise-end

    private void OnExamined(EntityUid uid, DragonRiftComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("carp-rift-examine", ("percentage", MathF.Round(component.Accumulator / component.MaxAccumulator * 100))));
    }

    private void OnAnchorChange(EntityUid uid, DragonRiftComponent component, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored && component.State == DragonRiftState.Charging)
        {
            QueueDel(uid);
        }
    }

    private void OnShutdown(EntityUid uid, DragonRiftComponent comp, ComponentShutdown args)
    {
        if (!TryComp<DragonComponent>(comp.Dragon, out var dragon) || dragon.Weakened)
            return;

        _dragon.RiftDestroyed(comp.Dragon.Value, dragon);
    }
}
