using System.Linq;
using Content.Shared._Sunrise.Weapons.Ranged;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.PneumaticCannon;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Weapons;
public sealed class EnergyGunFireModesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyGunFireModesComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<EnergyGunFireModesComponent, ActivateInWorldEvent>(OnInteractHandEvent);
        SubscribeLocalEvent<EnergyGunFireModesComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<EnergyGunFireModesComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<EnergyGunFireModesComponent, AttemptShootEvent>(OnAttemptShoot);
    }

    private void OnAttemptShoot(EntityUid uid, EnergyGunFireModesComponent component, ref AttemptShootEvent args)
    {
        if (HasComp<BallisticAmmoProviderComponent>(uid) || HasComp<ProjectileBatteryAmmoProviderComponent>(uid))
            return;

        if (component.FireModes.Count == 0)
            return;

        SetFireMode(uid, component, component.FireModes[0]);
    }

    private void OnStartup(Entity<EnergyGunFireModesComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.FireModes.Count == 0 || _entityManager.IsPaused(ent.Owner))
            return;

        SetFireMode(ent.Owner, ent.Comp, ent.Comp.FireModes[0]);
    }

    private void OnExamined(EntityUid uid, EnergyGunFireModesComponent fireModesComponent, ExaminedEvent args)
    {
        if (fireModesComponent.FireModes.Count < 2)
            return;

        if (fireModesComponent.CurrentFireMode == null)
        {
            SetFireMode(uid, fireModesComponent, fireModesComponent.FireModes.First());
        }

        if (fireModesComponent.CurrentFireMode == null)
            return;

        args.PushMarkup(Loc.GetString("energygun-examine-fire-mode", ("mode", Loc.GetString(fireModesComponent.CurrentFireMode.Name))));
    }

    private void OnGetVerb(EntityUid uid, EnergyGunFireModesComponent fireModesComponent, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (fireModesComponent.FireModes.Count < 2)
            return;

        if (fireModesComponent.CurrentFireMode == null)
        {
            SetFireMode(uid, fireModesComponent, fireModesComponent.FireModes.First());
        }

        foreach (var fireMode in fireModesComponent.FireModes)
        {
            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = Loc.GetString(fireMode.Name),
                Disabled = fireMode == fireModesComponent.CurrentFireMode,
                Impact = LogImpact.Low,
                DoContactInteraction = true,
                Act = () =>
                {
                    SetFireMode(uid, fireModesComponent, fireMode, args.User);
                }
            };

            args.Verbs.Add(v);
        }
    }

    private void OnInteractHandEvent(EntityUid uid, EnergyGunFireModesComponent fireModesComponent, ActivateInWorldEvent args)
    {
        if (fireModesComponent.FireModes.Count < 2)
            return;

        CycleFireMode(uid, fireModesComponent, args.User);
    }

    private void CycleFireMode(EntityUid uid, EnergyGunFireModesComponent fireModesComponent, EntityUid user)
    {
        var index = (fireModesComponent.CurrentFireMode != null) ? Math.Max(fireModesComponent.FireModes.IndexOf(fireModesComponent.CurrentFireMode), 0) + 1 : 1;

        EnergyGunFireMode? fireMode;

        fireMode = index >= fireModesComponent.FireModes.Count ? fireModesComponent.FireModes.FirstOrDefault() : fireModesComponent.FireModes[index];

        SetFireMode(uid, fireModesComponent, fireMode, user);
    }

    private void SetFireMode(EntityUid uid, EnergyGunFireModesComponent fireModesComponent, EnergyGunFireMode? fireMode, EntityUid? user = null)
    {
        if (fireMode == null)
            return;

        switch (fireMode.ShotType)
        {
            case ShotType.Projectile when fireMode.ProjectilePrototype != null:
            {
                fireModesComponent.CurrentFireMode = fireMode;
                RemComp<HitscanBatteryAmmoProviderComponent>(uid);
                var projectileBatteryAmmoProvider = EnsureComp<ProjectileBatteryAmmoProviderComponent>(uid);

                if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.ProjectilePrototype, out var projectilePrototype))
                    return;

                projectileBatteryAmmoProvider.Prototype = fireMode.ProjectilePrototype;
                projectileBatteryAmmoProvider.FireCost = fireMode.FireCost;
                Dirty(uid, projectileBatteryAmmoProvider);
                break;
            }
            case ShotType.Hitscan when fireMode.HitscanPrototype != null:
            {
                fireModesComponent.CurrentFireMode = fireMode;
                RemComp<ProjectileBatteryAmmoProviderComponent>(uid);
                var hitscanBatteryAmmoProvider = EnsureComp<HitscanBatteryAmmoProviderComponent>(uid);

                if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.HitscanPrototype, out var hitscanPrototype))
                    return;

                hitscanBatteryAmmoProvider.Prototype = fireMode.HitscanPrototype;
                hitscanBatteryAmmoProvider.FireCost = fireMode.FireCost;
                Dirty(uid, hitscanBatteryAmmoProvider);
                break;
            }
        }

        if (user != null && _net.IsServer)
        {
            _popupSystem.PopupEntity(Loc.GetString("gun-set-fire-mode", ("mode", Loc.GetString(fireMode.Name))), uid, user.Value);
        }

        if (fireMode.State == string.Empty)
            return;

        if (TryComp<AppearanceComponent>(uid, out var _) && TryComp<ItemComponent>(uid, out var item))
        {
            _item.SetHeldPrefix(uid, fireMode.State, false, item);
            switch (fireMode.State)
            {
                case "disabler":
                    UpdateAppearance(uid, EnergyGunFireModeState.Disabler);
                    break;
                case "lethal":
                    UpdateAppearance(uid, EnergyGunFireModeState.Lethal);
                    break;
                case "special":
                    UpdateAppearance(uid, EnergyGunFireModeState.Special);
                    break;
            }
        }
    }

    private void UpdateAppearance(EntityUid uid, EnergyGunFireModeState state)
    {
        _appearance.SetData(uid, EnergyGunFireModeVisuals.State, state);
    }
}
