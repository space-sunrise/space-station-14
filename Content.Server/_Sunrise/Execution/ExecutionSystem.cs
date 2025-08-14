using Content.Server.Kitchen.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._Starlight.Weapon.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared._Sunrise.Execution;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Server._Sunrise.Execution;

/// <summary>
///     Verb for violently murdering cuffed creatures.
/// </summary>
public sealed class ExecutionSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;

    private const float MeleeExecutionTimeModifier = 5.0f;
    private const float GunExecutionTime = 6.0f;
    private const float DamageModifier = 9.0f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharpComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsMelee);
        SubscribeLocalEvent<GunComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsGun);

        SubscribeLocalEvent<SharpComponent, ExecutionDoAfterEvent>(OnDoafterMelee);
        SubscribeLocalEvent<GunComponent, ExecutionDoAfterEvent>(OnDoafterGun);
    }

    private void OnGetInteractionVerbsMelee(
        EntityUid uid,
        SharpComponent component,
        GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var weapon = args.Using!.Value;
        var victim = args.Target;
        var suicide = attacker == victim;

        if (!CanExecuteWithMelee(weapon, victim, attacker))
            return;

        UtilityVerb verb = new()
        {
            Act = () =>
            {
                TryStartMeleeExecutionDoafter(weapon, victim, attacker);
            },
            Impact = LogImpact.High,
            Text = suicide ? Loc.GetString("suicide-verb-name") : Loc.GetString("execution-verb-name"),
            Message = suicide ? Loc.GetString("suicide-verb-message") : Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private void OnGetInteractionVerbsGun(
        EntityUid uid,
        GunComponent component,
        GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var weapon = args.Using!.Value;
        var victim = args.Target;
        var suicide = attacker == victim;

        if (!CanExecuteWithGun(weapon, victim, attacker))
            return;

        UtilityVerb verb = new()
        {
            Act = () =>
            {
                TryStartGunExecutionDoafter(weapon, victim, attacker);
            },
            Impact = LogImpact.High,

            Text = suicide ? Loc.GetString("suicide-verb-name") : Loc.GetString("execution-verb-name"),
            Message = suicide ? Loc.GetString("suicide-verb-message") : Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private bool CanExecuteWithAny(EntityUid weapon, EntityUid victim, EntityUid attacker)
    {
        if (attacker != victim)
            return false;

        // No point executing someone if they can't take damage
        if (!TryComp<DamageableComponent>(victim, out var damage))
            return false;

        // You can't execute something that cannot die
        if (!TryComp<MobStateComponent>(victim, out var mobState))
            return false;

        // You can't execute borgs
        if (TryComp<BorgChassisComponent>(victim, out var borgChassis))
            return false;

        // You're not allowed to execute dead people (no fun allowed)
        if (_mobStateSystem.IsDead(victim, mobState))
            return false;

        // You must be able to attack people to execute
        if (!_actionBlockerSystem.CanAttack(attacker, victim))
            return false;

        // The victim must be incapacitated to be executed
        if (victim != attacker && _actionBlockerSystem.CanInteract(victim, null))
            return false;

        // All checks passed
        return true;
    }

    private bool CanExecuteWithMelee(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (user != victim)
            return false;

        if (!CanExecuteWithAny(weapon, victim, user)) return false;

        // We must be able to actually hurt people with the weapon
        if (!TryComp<MeleeWeaponComponent>(weapon, out var melee) && melee!.Damage.GetTotal() > 0.0f)
            return false;

        return true;
    }

    private bool CanExecuteWithGun(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (user != victim)
            return false;

        if (!CanExecuteWithAny(weapon, victim, user)) return false;

        // We must be able to actually fire the gun
        if (!TryComp<GunComponent>(weapon, out var gun) && _gunSystem.CanShoot(gun!))
            return false;

        if (_containerSystem.TryGetContainer(weapon, "gun_chamber", out var chamberContainer))
        {
            foreach (var contained in chamberContainer.ContainedEntities)
            {
                if (TryComp<CartridgeAmmoComponent>(contained, out var cartridge) && cartridge.Spent)
                    return false;
            }
        }

        return true;
    }

    private void TryStartMeleeExecutionDoafter(EntityUid weapon, EntityUid victim, EntityUid attacker)
    {
        if (!CanExecuteWithMelee(weapon, victim, attacker))
            return;

        var executionTime = (1.0f / Comp<MeleeWeaponComponent>(weapon).AttackRate) * MeleeExecutionTimeModifier;

        if (attacker == victim)
        {
            ShowExecutionPopup("suicide-popup-melee-initial-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("suicide-popup-melee-initial-external", Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);
        }
        else
        {
            ShowExecutionPopup("execution-popup-melee-initial-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("execution-popup-melee-initial-external", Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);
        }

        var doAfter =
            new DoAfterArgs(EntityManager, attacker, executionTime, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
            };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private void TryStartGunExecutionDoafter(EntityUid weapon, EntityUid victim, EntityUid attacker)
    {
        if (!CanExecuteWithGun(weapon, victim, attacker))
            return;

        if (!TryComp<GunComponent>(weapon, out var gunComponent))
            return;

        var shotAttempted = new ShotAttemptedEvent
        {
            User = attacker,
            Used = (weapon, gunComponent),
        };

        RaiseLocalEvent(weapon, ref shotAttempted);
        if (shotAttempted.Cancelled)
        {
            if (shotAttempted.Message != null)
                _popupSystem.PopupEntity(shotAttempted.Message, weapon, attacker);
            return;
        }

        if (attacker == victim)
        {
            ShowExecutionPopup("suicide-popup-gun-initial-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("suicide-popup-gun-initial-external", Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);
        }
        else
        {
            ShowExecutionPopup("execution-popup-gun-initial-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("execution-popup-gun-initial-external", Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);
        }

        var doAfter =
            new DoAfterArgs(EntityManager, attacker, attacker == victim ? GunExecutionTime / 2 : GunExecutionTime, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private bool OnDoafterChecks(EntityUid uid, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return false;

        if (!CanExecuteWithAny(args.Used.Value, args.Target.Value, uid))
            return false;

        // All checks passed
        return true;
    }

    private void OnDoafterMelee(EntityUid uid, SharpComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;

        var attacker = args.User;
        var victim = args.Target!.Value;
        var weapon = args.Used!.Value;

        if (!CanExecuteWithMelee(weapon, victim, attacker)) return;

        if (!TryComp<MeleeWeaponComponent>(weapon, out var melee) && melee!.Damage.GetTotal() > 0.0f)
            return;

        _damageableSystem.TryChangeDamage(victim, melee.Damage * DamageModifier, true, useVariance: false, useModifier: false);
        _audioSystem.PlayEntity(melee.HitSound, Filter.Pvs(weapon), weapon, true, AudioParams.Default);

        if (attacker == victim)
        {
            ShowExecutionPopup("suicide-popup-melee-complete-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("suicide-popup-melee-complete-external", Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);
        }
        else
        {
            ShowExecutionPopup("execution-popup-melee-complete-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("execution-popup-melee-complete-external", Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);
        }
    }

    // TODO: This repeats a lot of the code of the serverside GunSystem, make it not do that
    private void OnDoafterGun(EntityUid uid, GunComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;

        var attacker = args.User;
        var weapon = args.Used.Value;
        var victim = args.Target.Value;

        if (!CanExecuteWithGun(weapon, victim, attacker))
            return;

        // Check if any systems want to block our shot
        var prevention = new ShotAttemptedEvent
        {
            User = attacker,
            Used = (weapon, component),
        };

        RaiseLocalEvent(weapon, ref prevention);
        if (prevention.Cancelled)
            return;

        RaiseLocalEvent(attacker, ref prevention);
        if (prevention.Cancelled)
            return;

        // Not sure what this is for but gunsystem uses it so ehhh
        var attemptEv = new AttemptShootEvent(attacker, null);
        RaiseLocalEvent(weapon, ref attemptEv);

        if (attemptEv.Cancelled)
        {
            if (attemptEv.Message != null)
                _popupSystem.PopupClient(attemptEv.Message, weapon, attacker);
            return;
        }

        // Take some ammunition for the shot (one bullet)
        var fromCoordinates = Transform(attacker).Coordinates;
        var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(), fromCoordinates, attacker);
        RaiseLocalEvent(weapon, ev);

        // Check if there's any ammo left
        if (ev.Ammo.Count <= 0)
        {
            _audioSystem.PlayEntity(component.SoundEmpty, Filter.Pvs(weapon), weapon, true, AudioParams.Default);
            ShowExecutionPopup("execution-popup-gun-empty", Filter.Pvs(weapon), PopupType.Medium, attacker, victim, weapon);
            return;
        }

        // Information about the ammo like damage
        DamageSpecifier damage = new DamageSpecifier();

        // Get some information from IShootable
        var ammoUid = ev.Ammo[0].Entity;
        switch (ev.Ammo[0].Shootable)
        {
            //🌟Starlight🌟 start
            case HitScanCartridgeAmmoComponent cartridge:
                var hitscanProto = _prototypeManager.Index(cartridge.Hitscan);
                if (hitscanProto.Damage is not null)
                    damage = hitscanProto.Damage * hitscanProto.Count;

                cartridge.Spent = true;
                _appearanceSystem.SetData(ammoUid!.Value, AmmoVisuals.Spent, true);
                Dirty(ammoUid.Value, cartridge);

                break;
            //🌟Starlight🌟 end
            case CartridgeAmmoComponent cartridge:
                // Get the damage value
                var prototype = _prototypeManager.Index<EntityPrototype>(cartridge.Prototype);
                prototype.TryGetComponent<ProjectileComponent>(out var projectileA, _componentFactory); // sloth forgive me
                if (projectileA != null)
                {
                    damage = projectileA.Damage;
                }
                prototype.TryGetComponent<ProjectileSpreadComponent>(out var projectilespreaderA, _componentFactory);
                if (projectilespreaderA != null)
                {
                    damage *= projectilespreaderA.Count;
                }

                // Expend the cartridge
                cartridge.Spent = true;
                _appearanceSystem.SetData(ammoUid!.Value, AmmoVisuals.Spent, true);
                Dirty(ammoUid.Value, cartridge);

                break;

            case AmmoComponent newAmmo:
                TryComp<ProjectileComponent>(ammoUid, out var projectileB);
                if (projectileB != null)
                {
                    damage = projectileB.Damage;
                }
                Del(ammoUid);
                break;

            case HitscanPrototype hitscan:
                damage = hitscan.Damage!;
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        // Gun successfully fired, deal damage
        _damageableSystem.TryChangeDamage(victim, damage * DamageModifier, true, useVariance: false, useModifier: false);
        _audioSystem.PlayEntity(component.SoundGunshot, Filter.Pvs(weapon), weapon, false, AudioParams.Default);

        // Popups
        if (attacker != victim)
        {
            ShowExecutionPopup("execution-popup-gun-complete-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("execution-popup-gun-complete-external", Filter.PvsExcept(attacker), PopupType.LargeCaution, attacker, victim, weapon);
        }
        else
        {
            ShowExecutionPopup("suicide-popup-gun-complete-internal", Filter.Entities(attacker), PopupType.LargeCaution, attacker, victim, weapon);
            ShowExecutionPopup("suicide-popup-gun-complete-external", Filter.PvsExcept(attacker), PopupType.LargeCaution, attacker, victim, weapon);
        }
    }

    private void ShowExecutionPopup(string locString, Filter filter, PopupType type,
        EntityUid attacker, EntityUid victim, EntityUid weapon)
    {
        _popupSystem.PopupEntity(Loc.GetString(
                locString, ("attacker", attacker), ("victim", victim), ("weapon", weapon)),
            attacker, filter, true, type);
    }
}
