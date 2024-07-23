using Content.Server.Administration.Logs;
using Content.Server.Audio;
using Content.Server.Power.Components;
using Content.Shared._Sunrise.Pacificator;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Pacificator;

public sealed class PacificatorSystems : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Pacificator.PacificatorComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<Pacificator.PacificatorComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<Pacificator.PacificatorComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<Pacificator.PacificatorComponent, SwitchGeneratorMessage>(
            OnSwitchGenerator);
    }

    private void OnInteractHand(EntityUid uid, Pacificator.PacificatorComponent component, InteractHandEvent args)
    {
        ApcPowerReceiverComponent? powerReceiver = default!;
        if (!Resolve(uid, ref powerReceiver))
            return;

        // Do not allow opening UI if broken or unpowered.
        if (!component.Intact || powerReceiver.PowerReceived < component.IdlePowerUse)
            return;

        _uiSystem.TryOpenUi(uid, PacificatorUiKey.Key, args.User);
        component.NeedUIUpdate = true;
    }

    private void OnComponentShutdown(EntityUid uid, Pacificator.PacificatorComponent component, ComponentShutdown args)
    {
        foreach (var pacifiedEntity in component.PacifiedEntities)
        {
            RemComp<PacifiedComponent>(pacifiedEntity);
            component.PacifiedEntities.Remove(pacifiedEntity);
        }
    }

    private void OnCompInit(Entity<Pacificator.PacificatorComponent> ent, ref ComponentInit args)
    {
        ApcPowerReceiverComponent? powerReceiver = null;
        if (!Resolve(ent, ref powerReceiver, false))
            return;

        UpdatePowerState(ent, powerReceiver);
        UpdateState((ent, ent.Comp, powerReceiver));
    }

    public void UpdateState(Entity<Pacificator.PacificatorComponent, ApcPowerReceiverComponent> ent)
    {
        var (uid, grav, powerReceiver) = ent;
        var appearance = EntityManager.GetComponentOrNull<AppearanceComponent>(uid);
        _appearance.SetData(uid, PacificatorVisuals.Charge, grav.Charge, appearance);

        if (_lights.TryGetLight(uid, out var pointLight))
        {
            _lights.SetEnabled(uid, grav.Charge > 0, pointLight);
            _lights.SetRadius(uid, MathHelper.Lerp(grav.LightRadiusMin, grav.LightRadiusMax, grav.Charge), pointLight);
        }

        if (!grav.Intact)
        {
            MakeBroken((uid, grav), appearance);
        }
        else if (powerReceiver.PowerReceived < grav.IdlePowerUse)
        {
            MakeUnpowered((uid, grav), appearance);
        }
        else if (!grav.SwitchedOn)
        {
            MakeOff((uid, grav), appearance);
        }
        else
        {
            MakeOn((uid, grav), appearance);
        }
    }

    private void MakeBroken(Entity<Pacificator.PacificatorComponent> ent, AppearanceComponent? appearance)
    {
        _ambientSoundSystem.SetAmbience(ent, false);

        _appearance.SetData(ent, PacificatorVisuals.State, PacificatorStatus.Broken);
    }

    private void MakeUnpowered(Entity<Pacificator.PacificatorComponent> ent, AppearanceComponent? appearance)
    {
        _ambientSoundSystem.SetAmbience(ent, false);

        _appearance.SetData(ent, PacificatorVisuals.State, PacificatorStatus.Unpowered, appearance);
    }

    private void MakeOff(Entity<Pacificator.PacificatorComponent> ent, AppearanceComponent? appearance)
    {
        _ambientSoundSystem.SetAmbience(ent, false);

        _appearance.SetData(ent, PacificatorVisuals.State, PacificatorStatus.Off, appearance);
    }

    private void MakeOn(Entity<Pacificator.PacificatorComponent> ent, AppearanceComponent? appearance)
    {
        _ambientSoundSystem.SetAmbience(ent, true);

        _appearance.SetData(ent, PacificatorVisuals.State, PacificatorStatus.On, appearance);
    }

    private void OnSwitchGenerator(
        EntityUid uid,
        Pacificator.PacificatorComponent component,
        SwitchGeneratorMessage args)
    {
        SetSwitchedOn(uid, args.Actor, component, args.On);
    }

    private void SetSwitchedOn(EntityUid uid, EntityUid actor, Pacificator.PacificatorComponent component, bool on,
        ApcPowerReceiverComponent? powerReceiver = null)
    {
        if (!Resolve(uid, ref powerReceiver))
            return;

        _adminLogger.Add(LogType.Action, on ? LogImpact.Medium : LogImpact.High, $"{actor:player} set ${ToPrettyString(uid):target} to {(on ? "on" : "off")}");

        component.SwitchedOn = on;
        UpdatePowerState(component, powerReceiver);
        component.NeedUIUpdate = true;
    }

    private static void UpdatePowerState(
        Pacificator.PacificatorComponent component,
        ApcPowerReceiverComponent powerReceiver)
    {
        powerReceiver.Load = component.SwitchedOn ? component.ActivePowerUse : component.IdlePowerUse;
    }

    private void UpdateUI(Entity<Pacificator.PacificatorComponent, ApcPowerReceiverComponent> ent, float chargeRate)
    {
        var (_, component, powerReceiver) = ent;
        if (!_uiSystem.IsUiOpen(ent.Owner, PacificatorUiKey.Key))
            return;

        var chargeTarget = chargeRate < 0 ? 0 : component.MaxCharge;
        short chargeEta;
        var atTarget = false;
        if (MathHelper.CloseTo(component.Charge, chargeTarget))
        {
            chargeEta = short.MinValue; // N/A
            atTarget = true;
        }
        else
        {
            var diff = chargeTarget - component.Charge;
            chargeEta = (short) Math.Abs(diff / chargeRate);
        }

        var status = chargeRate switch
        {
            > 0 when atTarget => PacificatorPowerStatus.FullyCharged,
            < 0 when atTarget => PacificatorPowerStatus.Off,
            > 0 => PacificatorPowerStatus.Charging,
            < 0 => PacificatorPowerStatus.Discharging,
            _ => throw new ArgumentOutOfRangeException()
        };

        var state = new GeneratorState(
            component.SwitchedOn,
            (byte) (component.Charge * 255),
            status,
            (short) Math.Round(powerReceiver.PowerReceived),
            (short) Math.Round(powerReceiver.Load),
            chargeEta
        );

        _uiSystem.SetUiState(
            ent.Owner,
            PacificatorUiKey.Key,
            state);

        component.NeedUIUpdate = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<Pacificator.PacificatorComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var pacificator, out var powerReceiver))
        {
            var ent = (uid, pacificator, powerReceiver);
            if (!pacificator.Intact)
                continue;

            // Calculate charge rate based on power state and such.
            // Negative charge rate means discharging.
            float chargeRate;
            if (pacificator.SwitchedOn)
            {
                if (powerReceiver.Powered)
                {
                    chargeRate = pacificator.ChargeRate;
                }
                else
                {
                    // Scale discharge rate such that if we're at 25% active power we discharge at 75% rate.
                    var receiving = powerReceiver.PowerReceived;
                    var mainSystemPower = Math.Max(0, receiving - pacificator.IdlePowerUse);
                    var ratio = 1 - mainSystemPower / (pacificator.ActivePowerUse - pacificator.IdlePowerUse);
                    chargeRate = -(ratio * pacificator.ChargeRate);
                }
            }
            else
            {
                chargeRate = -pacificator.ChargeRate;
            }

            var active = pacificator.Active;
            var lastCharge = pacificator.Charge;
            pacificator.Charge = Math.Clamp(pacificator.Charge + frameTime * chargeRate, 0, pacificator.MaxCharge);
            if (chargeRate > 0)
            {
                // Charging.
                if (MathHelper.CloseTo(pacificator.Charge, pacificator.MaxCharge) && !pacificator.Active)
                {
                    pacificator.Active = true;
                }
            }
            else
            {
                // Discharging
                if (MathHelper.CloseTo(pacificator.Charge, 0) && pacificator.Active)
                {
                    pacificator.Active = false;
                }
            }

            var updateUI = pacificator.NeedUIUpdate;
            if (!MathHelper.CloseTo(lastCharge, pacificator.Charge))
            {
                UpdateState(ent);
                updateUI = true;
            }

            if (updateUI)
                UpdateUI(ent, chargeRate);

            if (active != pacificator.Active)
            {
                if (!pacificator.Active)
                {
                    foreach (var pacifiedEntity in pacificator.PacifiedEntities)
                    {
                        RemComp<PacifiedComponent>(pacifiedEntity);
                        pacificator.PacifiedEntities.Remove(pacifiedEntity);
                    }
                }
            }

            UpdatePacified((ent.uid, ent.pacificator));
        }
    }

    private void UpdatePacified(Entity<Pacificator.PacificatorComponent> ent)
    {
        if (ent.Comp.NextTick > _timing.CurTime)
            return;

        ent.Comp.NextTick += ent.Comp.RefreshCooldown;

        if (!ent.Comp.Active)
            return;

        var coords = Transform(ent.Owner).Coordinates;

        var entities = _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(coords, ent.Comp.Range);

        foreach (var entityUid in entities)
        {
            if (ent.Comp.PacifiedEntities.Contains(entityUid))
                continue;

            EnsureComp<PacifiedComponent>(entityUid);
            ent.Comp.PacifiedEntities.Add(entityUid);
        }

        var entitiesToRemove = new HashSet<Entity<HumanoidAppearanceComponent>>();

        foreach (var pacifiedEntity in ent.Comp.PacifiedEntities)
        {
            if (entities.Contains(pacifiedEntity))
                continue;

            RemComp<PacifiedComponent>(pacifiedEntity);
            entitiesToRemove.Add(pacifiedEntity);
        }

        foreach (var entityToRemove in entitiesToRemove)
        {
            ent.Comp.PacifiedEntities.Remove(entityToRemove);
        }
    }
}

