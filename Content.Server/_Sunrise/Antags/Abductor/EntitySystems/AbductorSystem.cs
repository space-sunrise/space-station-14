using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.Antags.Abductor;
using Content.Shared.Eye;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Pinpointer;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Interaction.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.UserInterface;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Server.GameObjects;
using Content.Shared.Tag;
using Robust.Server.Containers;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Antags.Abductor;

public sealed partial class AbductorSystem : SharedAbductorSystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly TransformSystem _xformSys = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    private readonly EntProtoId _nanoStation = "StandardNanotrasenStation";

    private EntityUid? _opener;

    public override void Initialize()
    {
        SubscribeLocalEvent<AbductorHumanObservationConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
        SubscribeLocalEvent<AbductorHumanObservationConsoleComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttemptEvent);

        SubscribeLocalEvent<AbductorComponent, GetVisMaskEvent>(OnAbductorGetVis);

        Subs.BuiEvents<AbductorHumanObservationConsoleComponent>(AbductorCameraConsoleUIKey.Key, subs => subs.Event<AbductorBeaconChosenBuiMsg>(OnAbductorBeaconChosenBuiMsg));

        InitializeActions();
        InitializeGizmo();
        InitializeConsole();
        InitializeOrgans();
        InitializeVest();
        InitializeExtractor();
        base.Initialize();
    }

    private void OnAbductorGetVis(Entity<AbductorComponent> ent, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= (int)VisibilityFlags.Abductor;
    }

    private void OnAbductorBeaconChosenBuiMsg(Entity<AbductorHumanObservationConsoleComponent> ent, ref AbductorBeaconChosenBuiMsg args)
    {
        OnCameraExit(args.Actor);

        EntityUid eye;

        _opener = args.Actor;
        var beacon = _entityManager.GetEntity(args.Beacon.NetEnt);
        var beaconCoords = Transform(beacon).Coordinates;

        if (ent.Comp.RemoteEntity != null && TryGetEntity(ent.Comp.RemoteEntity, out var existingEye))
        {
            eye = existingEye.Value;
            _xformSys.SetCoordinates(eye, beaconCoords);
        }
        else
        {
            if (ent.Comp.RemoteEntityProto == null)
                return;

            eye = SpawnAtPosition(ent.Comp.RemoteEntityProto, Transform(beacon).Coordinates);
            ent.Comp.RemoteEntity = GetNetEntity(eye);

            EnsureComp<VisibilityComponent>(eye);

            var remoteEyeSource = EnsureComp<RemoteEyeSourceContainerComponent>(eye);
            remoteEyeSource.Actor = args.Actor;

            Dirty(eye, remoteEyeSource);
        }

        AddVirtualItems(args.Actor, ent);
        SetEye(args.Actor, eye, args);
        Dirty(ent);
    }
    private void OnCameraExit(EntityUid actor)
    {
        if (!HasComp<RelayInputMoverComponent>(actor))
            return;

        EntityUid? console = null;

        if (TryComp<AbductorScientistComponent>(actor, out var scientistComp) && scientistComp.Console is { } scientistConsole)
            console = scientistConsole;

        if (TryComp<AbductorAgentComponent>(actor, out var agentComp) && agentComp.Console is { } agentConsole)
            console = agentConsole;

        if (console == null)
            return;

        RemoveEye(actor);
        _virtualItem.DeleteInHandsMatching(actor, console.Value);

        _opener = null;
    }

    private void OnActivatableUIOpenAttemptEvent(Entity<AbductorHumanObservationConsoleComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<AbductorScientistComponent>(args.User) && !HasComp<AbductorAgentComponent>(args.User))
            args.Cancel();

        if (_opener != null && _opener != args.User)
        {
            _popup.PopupEntity(Loc.GetString("console-occupied"), args.User, args.User);
            args.Cancel();
        }
    }

    private void OnBeforeActivatableUIOpen(Entity<AbductorHumanObservationConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        if (TryComp<AbductorScientistComponent>(args.User, out var scientistComp))
            scientistComp.Console = ent.Owner;

        if (TryComp<AbductorAgentComponent>(args.User, out var agentComp))
            agentComp.Console = ent.Owner;

        var stations = _stationSystem.GetStations();
        var result = new Dictionary<int, StationBeacons>();

        foreach (var station in stations)
        {
            if (!TryComp(station, out MetaDataComponent? meta) || meta.EntityPrototype == null)
                continue;

            if (meta.EntityPrototype.ID != _nanoStation)
                continue;

            if (_stationSystem.GetLargestGrid(Comp<StationDataComponent>(station)) is not { } grid
                || !TryComp(station, out MetaDataComponent? stationMetaData))
                continue;

            if (!_entityManager.TryGetComponent<NavMapComponent>(grid, out var navMap))
                continue;

            result.Add(station.Id, new StationBeacons
            {
                Name = stationMetaData.EntityName,
                StationId = station.Id,
                Beacons = [.. navMap.Beacons.Values],
            });
        }

        _uiSystem.SetUiState(ent.Owner, AbductorCameraConsoleUIKey.Key, new AbductorCameraConsoleBuiState() { Stations = result });
    }

    private void SetEye(EntityUid uid, EntityUid eye, AbductorBeaconChosenBuiMsg args)
    {
        if (!TryComp(uid, out EyeComponent? eyeComp))
            return;

        AddComp(uid, new StationAiOverlayComponent { AllowCrossGrid = true });

        _eye.RefreshVisibilityMask(uid);
        _eye.SetTarget(uid, eye, eyeComp);
        _eye.SetDrawFov(uid, false);

        AddActions(args);
        _mover.SetRelay(uid, eye);
    }

    private void RemoveEye(EntityUid uid)
    {
        if (!TryComp(uid, out EyeComponent? eyeComp))
            return;

        RemComp<RelayInputMoverComponent>(uid);
        RemComp<StationAiOverlayComponent>(uid);

        _eye.SetTarget(uid, null);
        _eye.SetVisibilityMask(uid, eyeComp.VisibilityMask ^ (int)VisibilityFlags.Abductor, eyeComp);
        _eye.SetDrawFov(uid, true);

        RemoveActions(uid);
    }

    private void AddVirtualItems(EntityUid uid, EntityUid console)
    {
        if (!TryComp<HandsComponent>(uid, out var hands))
            return;

        foreach (var hand in _hands.EnumerateHands(uid, hands))
        {
            if (hand.HeldEntity == null || HasComp<UnremoveableComponent>(hand.HeldEntity))
                continue;

            _hands.DoDrop(uid, hand, true, hands);
        }

        if (_virtualItem.TrySpawnVirtualItemInHand(console, uid, out var virtItem1))
            EnsureComp<UnremoveableComponent>(virtItem1.Value);

        if (_virtualItem.TrySpawnVirtualItemInHand(console, uid, out var virtItem2))
            EnsureComp<UnremoveableComponent>(virtItem2.Value);
    }
}
