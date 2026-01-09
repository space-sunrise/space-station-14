using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._Starlight.Weapon.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared._Sunrise.Execution;
using Content.Shared.Body.Components;
using Content.Shared.Kitchen.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Body.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Content.Shared.Weapons.Hitscan.Components;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Execution;

/// <summary>
///     Verb for violently murdering cuffed creatures.
/// </summary>
public sealed partial class ExecutionSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const float MeleeExecutionTimeModifier = 5.0f;
    private const float GunExecutionTime = 6.0f;
    private const float SuicideGunTimeMultiplier = 0.5f;
    private const int GunExecutionShots = 1;
    private const float AttackRateToSeconds = 1.0f;
    private const float OverkillFractionMin = 0.05f;
    private const float OverkillFractionMax = 0.20f;
    private const float ExplosiveOverkillFractionMin = 2.0f;
    private const float ExplosiveOverkillFractionMax = 4.0f;
    private const float SuicideExplosionIntensityScale = 0.45f;
    private const int SuicideExplosionMaxTileBreak = 0;
    private const bool SuicideExplosionCanCreateVacuum = false;
    private const string GunChamberContainerId = "gun_chamber";
    private const string GunMagazineContainerId = "gun_magazine";
    private const string StructuralDamageType = "Structural";

    private static readonly string[] NonLethalAmmoIdTokens =
    {
        "Practice",
        "Rubber",
    };

    private readonly record struct SuicideExplosionInfo(
        ProtoId<ExplosionPrototype> ExplosionType,
        float TotalIntensity,
        float IntensitySlope,
        float MaxIntensity,
        float TileBreakScale);

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharpComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsMelee);
        SubscribeLocalEvent<GunComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsGun);

        SubscribeLocalEvent<SharpComponent, ExecutionDoAfterEvent>(OnDoafterMelee);
        SubscribeLocalEvent<GunComponent, ExecutionDoAfterEvent>(OnDoafterGun);
    }

    private void OnGetInteractionVerbsMelee(Entity<SharpComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!TryGetVerbContext(ref args, out var attacker, out var weapon, out var victim, out var suicide))
            return;

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

    private void OnGetInteractionVerbsGun(Entity<GunComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!TryGetVerbContext(ref args, out var attacker, out var weapon, out var victim, out var suicide))
            return;

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

    private void TryStartMeleeExecutionDoafter(EntityUid weapon, EntityUid victim, EntityUid attacker)
    {
        if (!CanExecuteWithMelee(weapon, victim, attacker))
            return;

        var executionTime = AttackRateToSeconds / Comp<MeleeWeaponComponent>(weapon).AttackRate * MeleeExecutionTimeModifier;

        var internalKey = attacker == victim
            ? "suicide-popup-melee-initial-internal"
            : "execution-popup-melee-initial-internal";

        var externalKey = attacker == victim
            ? "suicide-popup-melee-initial-external"
            : "execution-popup-melee-initial-external";

        ShowExecutionPopup(internalKey, Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
        ShowExecutionPopup(externalKey, Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);


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

        var internalKey = attacker == victim
            ? "suicide-popup-gun-initial-internal"
            : "execution-popup-gun-initial-internal";

        var externalKey = attacker == victim
            ? "suicide-popup-gun-initial-external"
            : "execution-popup-gun-initial-external";

        ShowExecutionPopup(internalKey, Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
        ShowExecutionPopup(externalKey, Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);

        var doAfter =
            new DoAfterArgs(EntityManager,
                attacker,
                attacker == victim ? GunExecutionTime * SuicideGunTimeMultiplier : GunExecutionTime,
                new ExecutionDoAfterEvent(),
                weapon,
                target: victim,
                used: weapon)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }


    private void OnDoafterMelee(Entity<SharpComponent> ent, ref ExecutionDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;

        var attacker = args.User;
        var victim = args.Target!.Value;
        var weapon = args.Used!.Value;

        if (!CanExecuteWithMelee(weapon, victim, attacker))
            return;

        if (!TryComp<MeleeWeaponComponent>(weapon, out var melee))
            return;

        ApplyExecutionDamage(victim, weapon, melee.Damage, forceLethal: true, OverkillFractionMin, OverkillFractionMax);
        _audioSystem.PlayEntity(melee.HitSound, Filter.Pvs(weapon), weapon, true, AudioParams.Default);

        var internalKey = attacker == victim
            ? "suicide-popup-melee-complete-internal"
            : "execution-popup-melee-complete-internal";

        var externalKey = attacker == victim
            ? "suicide-popup-melee-complete-external"
            : "execution-popup-melee-complete-external";

        ShowExecutionPopup(internalKey, Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
        ShowExecutionPopup(externalKey, Filter.PvsExcept(attacker), PopupType.MediumCaution, attacker, victim, weapon);
    }


    // TODO: This repeats a lot of the code of the serverside GunSystem, make it not do that
    private void OnDoafterGun(Entity<GunComponent> ent, ref ExecutionDoAfterEvent args)
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
            Used = (weapon, ent.Comp),
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
        var takeAmmoEvent = new TakeAmmoEvent(
            GunExecutionShots,
            new List<(EntityUid? Entity, IShootable Shootable)>(),
            fromCoordinates,
            attacker);
        RaiseLocalEvent(weapon, takeAmmoEvent);

        // Check if there's any ammo left
        if (takeAmmoEvent.Ammo.Count <= 0)
        {
            _audioSystem.PlayEntity(ent.Comp.SoundEmpty, Filter.Pvs(weapon), weapon, true, AudioParams.Default);
            ShowExecutionPopup("execution-popup-gun-empty", Filter.Pvs(weapon), PopupType.Medium, attacker, victim, weapon);
            return;
        }

        // Information about the ammo like damage
        DamageSpecifier damage = new();
        SuicideExplosionInfo? explosiveToTrigger = null;
        string? firedPrototypeId = null;

        // Get some information from IShootable
        var ammoUid = takeAmmoEvent.Ammo[0].Entity;
        var shootable = takeAmmoEvent.Ammo[0].Shootable;
        var isSpent = shootable switch
        {
            CartridgeAmmoComponent x => x.Spent,
            HitScanCartridgeAmmoComponent x => x.Spent,
            _ => false
        };

        if (isSpent)
        {
            if (ammoUid != null)
            {
                if (_containerSystem.TryGetContainer(weapon, GunChamberContainerId, out var chamberContainer))
                    _containerSystem.Remove(ammoUid.Value, chamberContainer);

                _transformSystem.DropNextTo(ammoUid.Value, weapon);
            }

            _audioSystem.PlayEntity(ent.Comp.SoundEmpty, Filter.Pvs(weapon), weapon, true, AudioParams.Default);
            ShowExecutionPopup("execution-popup-ammo-empty", Filter.Pvs(weapon), PopupType.Medium, attacker, victim, weapon);
            return;
        }


        switch (takeAmmoEvent.Ammo[0].Shootable)
        {
            //🌟Starlight🌟 start
            case HitScanCartridgeAmmoComponent cartridge:
                var hitscanProto = _prototypeManager.Index(cartridge.Hitscan);
                firedPrototypeId = cartridge.Hitscan.Id;

                if (hitscanProto.Damage is not null)
                    damage = hitscanProto.Damage * hitscanProto.Count;

                cartridge.Spent = true;
                _appearanceSystem.SetData(ammoUid!.Value, AmmoVisuals.Spent, true);

                Dirty(ammoUid.Value, cartridge);

                if (cartridge.DeleteOnSpawn)
                    Del(ammoUid.Value);

                break;
            //🌟Starlight🌟 end
            case CartridgeAmmoComponent cartridge:
                // Get the damage value
                var prototype = _prototypeManager.Index<EntityPrototype>(cartridge.Prototype);
                firedPrototypeId = cartridge.Prototype.Id;
                prototype.TryGetComponent<ProjectileComponent>(out var projectilePrototype, _componentFactory);

                if (projectilePrototype != null)
                    damage = projectilePrototype.Damage;

                else if (prototype.TryGetComponent<HitscanBasicDamageComponent>(out var hitscanDamage, _componentFactory) && hitscanDamage != null)
                    damage = hitscanDamage.Damage;

                if (prototype.TryGetComponent<ExplosiveComponent>(out var explosiveProto, _componentFactory) && explosiveProto != null)
                {
                    explosiveToTrigger = new SuicideExplosionInfo(
                        explosiveProto.ExplosionType,
                        explosiveProto.TotalIntensity,
                        explosiveProto.IntensitySlope,
                        explosiveProto.MaxIntensity,
                        explosiveProto.TileBreakScale);
                }

                prototype.TryGetComponent<ProjectileSpreadComponent>(out var projectileSpread, _componentFactory);

                if (projectileSpread != null)
                    damage *= projectileSpread.Count;

                // Expend the cartridge
                cartridge.Spent = true;
                _appearanceSystem.SetData(ammoUid!.Value, AmmoVisuals.Spent, true);
                Dirty(ammoUid.Value, cartridge);

                if (cartridge.DeleteOnSpawn)
                    Del(ammoUid.Value);

                break;

            case AmmoComponent:
                if (ammoUid != null)
                    firedPrototypeId = MetaData(ammoUid.Value).EntityPrototype?.ID;

                TryComp<ProjectileComponent>(ammoUid, out var projectileAmmo);

                if (projectileAmmo != null)
                    damage = projectileAmmo.Damage;

                else if (ammoUid != null && TryComp<HitscanBasicDamageComponent>(ammoUid, out var hitscanAmmoDamage) && !hitscanAmmoDamage.Damage.Empty)
                    damage = hitscanAmmoDamage.Damage;

                if (ammoUid != null && TryComp<ExplosiveComponent>(ammoUid.Value, out var explosiveAmmo))
                {
                    explosiveToTrigger = new SuicideExplosionInfo(
                        explosiveAmmo.ExplosionType,
                        explosiveAmmo.TotalIntensity,
                        explosiveAmmo.IntensitySlope,
                        explosiveAmmo.MaxIntensity,
                        explosiveAmmo.TileBreakScale);
                }

                if (ammoUid != null)
                    Del(ammoUid.Value);

                break;

            case HitscanAmmoComponent:
                if (ammoUid != null)
                    firedPrototypeId = MetaData(ammoUid.Value).EntityPrototype?.ID;

                if (ammoUid != null && TryComp<HitscanBasicDamageComponent>(ammoUid, out var hitscanAmmo) && !hitscanAmmo.Damage.Empty)
                    damage = hitscanAmmo.Damage;

                if (ammoUid != null)
                    Del(ammoUid.Value);

                break;

            case HitscanPrototype hitscan:
                firedPrototypeId = hitscan.ID;
                damage = hitscan.Damage!;
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        var forceLethal = !IsNonLethalAmmo(firedPrototypeId);
        var isExplosive = explosiveToTrigger != null;

        if (isExplosive && forceLethal)
        {
            var explosionType = _prototypeManager.Index(explosiveToTrigger!.Value.ExplosionType);
            ApplyExecutionDamage(victim, weapon, explosionType.DamagePerIntensity, forceLethal: true, ExplosiveOverkillFractionMin, ExplosiveOverkillFractionMax);

            if (TryComp<BloodstreamComponent>(victim, out var bloodstream))
                _bloodstreamSystem.SpillAllSolutions((victim, bloodstream));
        }

        else
            ApplyExecutionDamage(victim, weapon, damage, forceLethal, OverkillFractionMin, OverkillFractionMax);

        if (explosiveToTrigger != null)
        {
            var explosive = explosiveToTrigger.Value;
            _explosionSystem.QueueExplosion(
                victim,
                explosive.ExplosionType,
                explosive.TotalIntensity * SuicideExplosionIntensityScale,
                explosive.IntensitySlope,
                explosive.MaxIntensity * SuicideExplosionIntensityScale,
                explosive.TileBreakScale,
                SuicideExplosionMaxTileBreak,
                SuicideExplosionCanCreateVacuum,
                attacker);
        }
        _audioSystem.PlayEntity(ent.Comp.SoundGunshot, Filter.Pvs(weapon), weapon, false, AudioParams.Default);

        // Popups

        var internalKey = attacker != victim
            ? "execution-popup-gun-complete-internal"
            : "suicide-popup-gun-complete-internal";

        var externalKey = attacker != victim
            ? "execution-popup-gun-complete-external"
            : "suicide-popup-gun-complete-external";


        ShowExecutionPopup(internalKey, Filter.Entities(attacker), PopupType.LargeCaution, attacker, victim, weapon);
        ShowExecutionPopup(externalKey, Filter.PvsExcept(attacker), PopupType.LargeCaution, attacker, victim, weapon);
    }

}
