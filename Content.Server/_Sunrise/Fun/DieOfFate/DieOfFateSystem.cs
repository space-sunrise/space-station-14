using System.Linq;
using System.Numerics;
using Content.Server.Antag;
using Content.Server.Body.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Raffles;
using Content.Server.Humanoid.Systems;
using Content.Shared.Ghost.Roles.Raffles;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Dice;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Fun.DieOfFate;

public sealed class DieOfFateSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly RandomHumanoidSystem _randomHumanoid = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    // Spells that can be granted without requiring wizard robes
    private static readonly string[] AvailableSpells =
    {
        "ActionFireball",
        "ActionBlink",
        "ActionSmoke",
        "ActionForceWall",
        "ActionKnock",
        "ActionRepulse",
        "ActionSpawnMagicarpSpell",
        "ActionAnimateSpell",
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DieOfFateComponent, UseInHandEvent>(OnUseInHand, after: [typeof(SharedDiceSystem)]);
        SubscribeLocalEvent<DieOfFateComponent, LandEvent>(OnLand, after: [typeof(SharedDiceSystem)]);
        SubscribeLocalEvent<DieOfFateComponent, ExaminedEvent>(OnExamine);
    }

    private void OnUseInHand(Entity<DieOfFateComponent> ent, ref UseInHandEvent args)
    {
        RollAndApply(ent, args.User);
    }

    private void OnLand(Entity<DieOfFateComponent> ent, ref LandEvent args)
    {
        if (args.User is { } user)
            RollAndApply(ent, user);
    }

    private void OnExamine(Entity<DieOfFateComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.MaxUsesPerPlayer < 0)
            return;

        if (ent.Comp.RollsByPlayer.TryGetValue(args.Examiner, out var rolls)
            && rolls >= ent.Comp.MaxUsesPerPlayer)
        {
            args.PushMarkup(Loc.GetString("die-of-fate-examine-inert"));
        }
    }

    private void RollAndApply(Entity<DieOfFateComponent> die, EntityUid user)
    {
        // Per-player use tracking
        if (die.Comp.MaxUsesPerPlayer >= 0)
        {
            die.Comp.RollsByPlayer.TryGetValue(user, out var currentRolls);
            if (currentRolls >= die.Comp.MaxUsesPerPlayer)
                return;
        }

        // Get the dice roll value that was already set by SharedDiceSystem
        if (!TryComp<DiceComponent>(die, out var dice))
            return;

        var roll = dice.CurrentValue;

        // Update per-player roll count
        die.Comp.RollsByPlayer.TryGetValue(user, out var rolls);
        die.Comp.RollsByPlayer[user] = rolls + 1;

        ApplyEffect(die, user, roll);
    }

    private void ApplyEffect(EntityUid die, EntityUid user, int roll)
    {
        var coords = Transform(user).Coordinates;

        switch (roll)
        {
            case 1: // Dusted - turn everything to ash
                RollDust(user, coords);
                break;
            case 2: // Gibbed
                RollGib(user);
                break;
            case 3: // Hostile creatures spawn
                RollHostileCreatures(user, coords);
                break;
            case 4: // Destroy all items
                RollDestroyItems(user);
                break;
            case 5: // Monkey
                RollMonkey(user);
                break;
            case 6: // Slow
                RollSlow(user);
                break;
            case 7: // Thrown randomly with stun and damage
                RollThrown(user);
                break;
            case 8: // Explosion (but survive)
                RollExplosion(user);
                break;
            case 9: // Cold/sickness
                RollCold(user);
                break;
            case 10: // Nothing
                RollNothing(user);
                break;
            case 11: // Cookie
                RollCookie(user, coords);
                break;
            case 12: // Full heal
                RollHeal(user);
                break;
            case 13: // Money
                RollMoney(user, coords);
                break;
            case 14: // Revolver
                RollRevolver(user, coords);
                break;
            case 15: // Random spell
                RollSpell(user);
                break;
            case 16: // Familiar
                RollFamiliar(user, coords);
                break;
            case 17: // Surplus crate
                RollSurplusCrate(user, coords);
                break;
            case 18: // Captain ID
                RollCaptainId(user, coords);
                break;
            case 19: // Armor boost
                RollArmorBoost(user);
                break;
            case 20: // Wizard
                RollWizard(user);
                break;
        }
    }

    // Roll 1: Turn everything to ash
    private void RollDust(EntityUid user, EntityCoordinates coords)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-1"), user, PopupType.LargeCaution);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/burning.ogg"), user);

        // Recursively delete the player and every item they carry (including nested containers)
        DeleteEntityAndContents(user);
        Spawn("Ash", coords);
    }

    /// <summary>
    /// Queues deletion of <paramref name="uid"/> and every entity nested inside its containers,
    /// so nothing survives as orphaned floor items.
    /// </summary>
    private void DeleteEntityAndContents(EntityUid uid)
    {
        if (TryComp<ContainerManagerComponent>(uid, out var containerManager))
        {
            foreach (var container in _container.GetAllContainers(uid, containerManager))
            {
                foreach (var contained in container.ContainedEntities.ToArray())
                {
                    DeleteEntityAndContents(contained);
                }
            }
        }
        QueueDel(uid);
    }

    // Roll 2: Gib
    private void RollGib(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-2"), user, PopupType.LargeCaution);
        if (TryComp<BodyComponent>(user, out _))
            _body.GibBody(user, gibOrgans: true);
    }

    // Roll 3: Hostile creatures
    private void RollHostileCreatures(EntityUid user, EntityCoordinates coords)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-3"), user, PopupType.LargeCaution);
        var mobs = new[] { "MobCarpMagic", "MobCarpMagic", "MobCarpMagic", "MobCarpMagic", "MobCarpMagic" };
        foreach (var mob in mobs)
        {
            var offset = _random.NextVector2Box(-2f, -2f, 2f, 2f);
            Spawn(mob, coords.Offset(offset));
        }
    }

    // Roll 4: Destroy all items (turn to ash)
    private void RollDestroyItems(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-4"), user, PopupType.LargeCaution);
        var coords = Transform(user).Coordinates;

        var toDestroy = new List<EntityUid>();

        if (_inventory.TryGetSlots(user, out var slotDefinitions))
        {
            foreach (var slot in slotDefinitions)
            {
                if (_inventory.TryGetSlotEntity(user, slot.Name, out var slotEnt))
                {
                    _inventory.TryUnequip(user, slot.Name, true, true);
                    toDestroy.Add(slotEnt.Value);
                }
            }
        }

        foreach (var held in _hands.EnumerateHeld(user).ToList())
        {
            _hands.TryDrop(user, held);
            toDestroy.Add(held);
        }

        foreach (var item in toDestroy)
        {
            DeleteEntityAndContents(item);
            Spawn("Ash", coords);
        }
    }

    // Roll 5: Monkey
    private void RollMonkey(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-5"), user, PopupType.LargeCaution);
        _polymorph.PolymorphEntity(user, "DieOfFateMonkeySmite");
    }

    // Roll 6: Slow
    private void RollSlow(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-6"), user, PopupType.Medium);
        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(user);
        _movementSpeed.ChangeBaseSpeed(user, movementSpeed.BaseWalkSpeed * 0.8f, movementSpeed.BaseSprintSpeed * 0.8f, movementSpeed.Acceleration, movementSpeed);
    }

    // Roll 7: Thrown in random direction with stun and brute damage
    private void RollThrown(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-7"), user, PopupType.LargeCaution);
        var direction = _random.NextVector2().Normalized();

        // Defer TryThrow to the next tick: if this roll triggers from OnLand (die was thrown),
        // ThrownItemSystem.Update is currently iterating ThrownItemComponent entities.
        // Calling TryThrow here would add ThrownItemComponent to 'user', modifying that collection
        // mid-iteration and causing an InvalidOperationException crash.
        var userCopy = user;
        var dirCopy = direction;
        Timer.Spawn(_gameTiming.TickPeriod, () =>
        {
            if (Deleted(userCopy))
                return;
            _throwing.TryThrow(userCopy, dirCopy * 10f, 10f);
        }, CancellationToken.None);

        _stun.TryAddParalyzeDuration(user, TimeSpan.FromSeconds(5));

        var damage = new DamageSpecifier();
        damage.DamageDict["Blunt"] = FixedPoint2.New(30);
        _damageable.TryChangeDamage(user, damage);
    }

    // Roll 8: Explosion like a weldertank (but survive)
    private void RollExplosion(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-8"), user, PopupType.LargeCaution);
        var coords = _transform.GetMapCoordinates(user);
        _explosion.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
            4, 1, 2, user, maxTileBreak: 0);
    }

    // Roll 9: Cold/sickness - apply toxin and cold damage
    private void RollCold(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-9"), user, PopupType.Medium);
        var damage = new DamageSpecifier();
        damage.DamageDict["Poison"] = FixedPoint2.New(15);
        damage.DamageDict["Cold"] = FixedPoint2.New(10);
        _damageable.TryChangeDamage(user, damage);
    }

    // Roll 10: Nothing
    private void RollNothing(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-10"), user, PopupType.Medium);
    }

    // Roll 11: Cookie
    private void RollCookie(EntityUid user, EntityCoordinates coords)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-11"), user, PopupType.Medium);
        var cookie = Spawn("FoodBakedCookie", coords);
        _hands.TryPickupAnyHand(user, cookie);
    }

    // Roll 12: Full heal
    private void RollHeal(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-12"), user, PopupType.Large);
        _damageable.SetAllDamage(user, FixedPoint2.Zero);
    }

    // Roll 13: Money
    private void RollMoney(EntityUid user, EntityCoordinates coords)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-13"), user, PopupType.Large);
        for (var i = 0; i < 10; i++)
        {
            var offset = _random.NextVector2Box(-1.5f, -1.5f, 1.5f, 1.5f);
            Spawn("SpaceCash1000", coords.Offset(offset));
        }
    }

    // Roll 14: Revolver
    private void RollRevolver(EntityUid user, EntityCoordinates coords)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-14"), user, PopupType.Large);
        var gun = Spawn("WeaponRevolverMateba", coords);
        _hands.TryPickupAnyHand(user, gun);
    }

    // Roll 15: Random spell
    private void RollSpell(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-15"), user, PopupType.Large);
        var spell = _random.Pick(AvailableSpells);
        _actions.AddAction(user, spell);
    }

    // Roll 16: Familiar (spawn a humanoid servant ghost role)
    private void RollFamiliar(EntityUid user, EntityCoordinates coords)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-16"), user, PopupType.Large);

        var masterName = MetaData(user).EntityName;

        // Spawn a random humanoid with service worker gear
        var offset = _random.NextVector2Box(-1f, -1f, 1f, 1f);
        var familiar = _randomHumanoid.SpawnRandomHumanoid("DieOfFateFamiliar", coords.Offset(offset), Loc.GetString("die-of-fate-familiar-name"));

        // Make sentient so a ghost can take over
        _mind.MakeSentient(familiar);

        // Set up ghost role so a ghost can take control, with raffle timer
        var ghostRole = EnsureComp<GhostRoleComponent>(familiar);
        ghostRole.RoleName = Loc.GetString("die-of-fate-familiar-role-name");
        ghostRole.RoleDescription = Loc.GetString("die-of-fate-familiar-role-desc", ("master", masterName));
        ghostRole.RoleRules = Loc.GetString("ghost-role-information-familiar-rules");
        ghostRole.MindRoles = new List<EntProtoId> { "MindRoleGhostRoleFamiliar" };
        ghostRole.RaffleConfig = new GhostRoleRaffleConfig(new GhostRoleRaffleSettings
        {
            InitialDuration = 30,
            JoinExtendsDurationBy = 2,
            MaxDuration = 30,
        });
        EnsureComp<GhostTakeoverAvailableComponent>(familiar);
    }

    // Roll 17: Surplus crate
    private void RollSurplusCrate(EntityUid user, EntityCoordinates coords)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-17"), user, PopupType.Large);
        Spawn("CrateSyndicateSurplusBundle", coords);
    }

    // Roll 18: Captain ID
    private void RollCaptainId(EntityUid user, EntityCoordinates coords)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-18"), user, PopupType.Large);
        var id = Spawn("CaptainIDCard", coords);
        _hands.TryPickupAnyHand(user, id);
    }

    // Roll 19: Armor boost
    private void RollArmorBoost(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-19"), user, PopupType.Large);
        _damageable.SetDamageModifierSetId(user, "DieOfFateArmor");
    }

    // Roll 20: Wizard
    private void RollWizard(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("die-of-fate-roll-20"), user, PopupType.LargeCaution);

        if (TryComp<ActorComponent>(user, out var actor))
        {
            _antag.ForceMakeAntag<WizardRoleComponent>(actor.PlayerSession, "Wizard");
        }
    }
}
