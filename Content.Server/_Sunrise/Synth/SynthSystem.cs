using System.Diagnostics.CodeAnalysis;
using Content.Server.Body.Systems;
using Content.Server.Emp;
using Content.Server.Humanoid;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Rejuvenate;
using Content.Shared.Rounding;
using Content.Shared.Stunnable;
using Content.Shared.Synth.Components;
using Content.Shared.Synth.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Synth;

public sealed class SynthSystem : SharedSynthSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly BatterySystem _batterySystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _sharedHuApp = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("Synth");

        SubscribeLocalEvent<SynthComponent, SetupOrgansEvent>(OnSetupOrgans);
        SubscribeLocalEvent<SynthComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<SynthComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<SynthComponent, SynthDrainPowerDoAfterEvent>(OnDrainDoAfter);
        SubscribeLocalEvent<ApcComponent, GetVerbsEvent<AlternativeVerb>>(AddDrainVerb);
        SubscribeLocalEvent<SynthComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<SynthComponent, SynthChangeScreenActionEvent>(OnSynthChangeScreen);
        SubscribeLocalEvent<SynthComponent, SynthScreenPrototypeSelectedMessage>(OnSynthScreenSelected);
        SubscribeLocalEvent<SynthComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SynthComponent, SynthDrainWiresActionEvent>(OnDrainWires);
    }

    private void OnMapInit(EntityUid uid, SynthComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, "SynthChangeScreen");
        _action.AddAction(uid, "SynthDrainWires");
    }

    private void TryOpenUi(EntityUid uid, EntityUid user, SynthComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp(user, out ActorComponent? actor))
            return;

        _uiSystem.TryToggleUi(uid, SynthScreenUiKey.Key, actor.PlayerSession);
        UpdateUi(uid, component);
    }

    private void OnSynthChangeScreen(EntityUid uid, SynthComponent observerComponent, SynthChangeScreenActionEvent args)
    {
        TryOpenUi(uid, args.Performer, observerComponent);
        args.Handled = true;
    }

    private void OnSynthScreenSelected(EntityUid uid, SynthComponent component, SynthScreenPrototypeSelectedMessage args)
    {
        _sharedHuApp.SetMarkingId(uid, MarkingCategories.Snout, 0, args.SelectedId);
    }

    private void OnDrainWires(EntityUid uid, SynthComponent component, SynthDrainWiresActionEvent args)
    {
        component.WiresExtended = !component.WiresExtended;
    
        if (component.WiresExtended)
        {
            _popup.PopupEntity(Loc.GetString("synth-wires-extended"), args.Performer, args.Performer, PopupType.Medium);
            _audioSystem.PlayPredicted(component.ExtendSound, uid, uid);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("synth-wires-cleared"), args.Performer, args.Performer, PopupType.Medium);
            _audioSystem.PlayPredicted(component.UnextendSound, uid, uid);
        }
    }

    private void UpdateUi(EntityUid uid, SynthComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var prototypes = _prototypeManager.EnumeratePrototypes<MarkingPrototype>();

        var screensList = new List<string>();

        foreach (var proto in prototypes)
        {
            if (proto.SpeciesRestrictions == null)
                continue;

            if (proto.MarkingCategory != MarkingCategories.Snout)
                continue;

            if (proto.SpeciesRestrictions.Contains("Synth"))
                screensList.Add(proto.ID);
        }

        var state = new SynthScreenBoundUserInterfaceState(screensList);

        _userInterfaceSystem.SetUiState(uid, SynthScreenUiKey.Key, state);
    }

    private void OnMobStateChanged(EntityUid uid, SynthComponent component, MobStateChangedEvent args)
    {
        switch (args.NewMobState)
        {
            case MobState.Dead:
            {
                _audioSystem.PlayPvs(component.DeathSound, uid);
                break;
            }
        }
    }

    private void OnSetupOrgans(EntityUid uid, SynthComponent component, SetupOrgansEvent ev)
    {
        if (!TryGetBattery(uid, out var battery, out var batteryComponent))
            return;

        component.Energy = batteryComponent.CurrentCharge;
        component.MaxEnergy = batteryComponent.MaxCharge;
        Dirty(uid, component);
    }

    private void OnEmpPulse(EntityUid uid, SynthComponent component, EmpPulseEvent ev)
    {
        _damageableSystem.TryChangeDamage(uid, component.EmpDamage, true);
        _stunSystem.TryParalyze(uid, TimeSpan.FromSeconds(component.EmpParalyzeTime), true);
    }

    private void AddDrainVerb(EntityUid uid, ApcComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<SynthComponent>(args.User, out var synthComponent))
            return;

        if (!synthComponent.WiresExtended)
            return;

        if (synthComponent.Energy >= synthComponent.MaxEnergy)
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                OnDrain(uid, args.User, synthComponent);
            },
            Text = Loc.GetString("synth-drain-verb"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")),
            Priority = 1
        };
        
        args.Verbs.Add(verb);
    }

    private void OnDrain(EntityUid uid, EntityUid user, SynthComponent component)
    {
        if (!_interactionSystem.InRangeUnobstructed(user, uid, 1.25f))
        {
            _popup.PopupEntity(Loc.GetString("robot-drain-too-far"), user);
            return;
        }

        if (!TryGetBattery(user, out var battery, out var batteryComponent))
        {
            _popup.PopupEntity(Loc.GetString("robot-drain-battery-none"), user);
            return;
        }

        if (batteryComponent.CurrentCharge >= batteryComponent.MaxCharge * 0.9)
        {
            _popup.PopupEntity(Loc.GetString("robot-drain-charge-full"), user, user, PopupType.Medium);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, user, component.PowerDrainDelay, new SynthDrainPowerDoAfterEvent(), target: uid, used: user, eventTarget: user)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.5f,
            CancelDuplicate = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private bool TryGetBattery(EntityUid uid, [NotNullWhen(true)] out EntityUid? powercell, [NotNullWhen(true)] out BatteryComponent? batteryComponent)
    {
        powercell = null;
        batteryComponent = null;
        
        if (!TryComp(uid, out ContainerManagerComponent? containerComp))
            return false;

        if (_containerSystem.TryGetContainer(uid, "cell_slot", out var container, containerComp) && container.ContainedEntities.Count > 0)
        {
            foreach (var content in container.ContainedEntities)
            {
                if (HasComp<PowerCellComponent>(content) && TryComp<BatteryComponent>(content, out var batteryComp))
                {
                    powercell = content;
                    batteryComponent = batteryComp;

                    return true;
                }
            }
        }

        return false;
    }

    private void OnDrainDoAfter(EntityUid uid, SynthComponent comp, SynthDrainPowerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        if (!TryGetBattery(args.User, out var battery, out var batteryComponent))
            return;

        if (!HasComp<ApcComponent>(args.Target.Value) ||
            !TryComp<BatteryComponent>(args.Target.Value, out var apcBattery))
            return;

        if (MathHelper.CloseToPercent(apcBattery.CurrentCharge, 0))
        {
            _popup.PopupEntity(Loc.GetString("robot-drain-charge-empty", ("battery", args.Target.Value)), args.User, args.User, PopupType.Medium);
            return;
        }

        var available = apcBattery.CurrentCharge;
        var input = Math.Min(available, comp.DrainPerUse / comp.DrainEfficiency);
        if (!_batterySystem.TryUseCharge(args.Target.Value, input, apcBattery))
            return;
        var output = input * comp.DrainEfficiency;
        TryChangeEnergy(args.User, output, comp);
        _popup.PopupEntity(Loc.GetString("robot-drain-charge-success", ("battery", args.Target.Value)), args.User, args.User);
        // TODO: spark effects
        _audioSystem.PlayPvs(comp.SparkSound, args.Target.Value);

        args.Repeat = (batteryComponent.CurrentCharge <= batteryComponent.MaxCharge * 0.95);
    }

    private void OnRejuvenate(EntityUid uid, SynthComponent component, RejuvenateEvent args)
    {
        if (!TryGetBattery(uid, out var battery, out var batteryComponent))
            return;

        _batterySystem.SetCharge(battery.Value, batteryComponent.MaxCharge, batteryComponent);
        component.Energy = batteryComponent.MaxCharge;
        Dirty(uid, component);
    }

    private bool TryChangeEnergy(EntityUid uid, FixedPoint2 delta, SynthComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!TryGetBattery(uid, out var battery, out var batteryComponent))
            return false;

        var newEnergy = FixedPoint2.Clamp(component.Energy + delta, 0, component.MaxEnergy);

        if (newEnergy != component.Energy)
        {
            _batterySystem.SetCharge(battery.Value, newEnergy.Float(), batteryComponent);
            component.Energy = newEnergy.Float();
            component.MaxEnergy = batteryComponent.MaxCharge;
        }

        switch (component.SlowState)
        {
            case SynthComponent.SlowStates.Off:
                if (component.Energy < component.MaxEnergy * component.EnergyLowSlowdownPercent)
                {
                    component.SlowState = SynthComponent.SlowStates.On;
                    _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
                }
                break;
            case SynthComponent.SlowStates.On:
                if (component.Energy > component.MaxEnergy * component.EnergyLowSlowdownPercent)
                {
                    component.SlowState = SynthComponent.SlowStates.Off;
                    _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
                }
                break;
        }
        
        Dirty(uid, component);

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var synthQuery = EntityQueryEnumerator<SynthComponent, MobStateComponent>();
        while (synthQuery.MoveNext(out var ent, out var compSynth, out var mobStateComponent))
        {
            if (mobStateComponent.CurrentState == MobState.Dead)
                continue;

            if (_timing.CurTime < compSynth.NextUpdateTime)
                continue;

            compSynth.NextUpdateTime = _timing.CurTime + compSynth.UpdateRate;

            TryChangeEnergy(ent, compSynth.EnergyСonsumption, compSynth);
        }
    }
}
