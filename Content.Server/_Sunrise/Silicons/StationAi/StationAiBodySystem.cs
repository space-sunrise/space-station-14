using System.Diagnostics.CodeAnalysis;
using Content.Server.Silicons.Laws;
using Content.Server.Silicons.Borgs;
using Content.Shared._Sunrise.Silicons.StationAi;
using Content.Shared.Actions;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Containers;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Movement.Components;
using Content.Shared.NameIdentifier;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.Silicons.StationAi;

/// <summary>
/// Server-side authority for preparing and controlling station AI bodies.
/// </summary>
public sealed class StationAiBodySystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly BorgSwitchableTypeSystem _borgSwitchableType = default!;
    [Dependency] private readonly SiliconLawSystem _siliconLaw = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private const string BodyUiClientType = "StationAiBodyBoundUserInterface";

    private static readonly EntProtoId BodyMenuAction = "ActionStationAiBodyMenu";
    private static readonly EntProtoId BodyExitAction = "ActionStationAiBodyExit";
    private static readonly SpriteSpecifier BodyEnterVerbIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png"));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgChassisComponent, StationAiBodyBoardInsertedEvent>(OnBodyBoardInserted);
        SubscribeLocalEvent<BorgChassisComponent, StationAiBodyBoardRemovedEvent>(OnBodyBoardRemoved);
        SubscribeLocalEvent<StationAiBodyControllerComponent, ComponentStartup>(OnStationAiBodyControllerStartup);
        SubscribeLocalEvent<StationAiBodyControllerComponent, MapInitEvent>(OnStationAiBodyControllerMapInit);
        SubscribeLocalEvent<StationAiBodyControllerComponent, ComponentShutdown>(OnStationAiBodyControllerShutdown);
        SubscribeLocalEvent<StationAiBodyControllerComponent, EntGotRemovedFromContainerMessage>(OnStationAiBrainGotRemovedFromCore);
        SubscribeLocalEvent<StationAiBodyControllerComponent, MobStateChangedEvent>(OnStationAiBrainMobStateChanged);
        SubscribeLocalEvent<StationAiBodyControllerComponent, StationAiBodyOpenUiActionEvent>(OnStationAiOpenBodyUiAction);
        SubscribeLocalEvent<StationAiBodyComponent, EntityTerminatingEvent>(OnBodyTerminating);
        SubscribeLocalEvent<StationAiBodyComponent, GetSiliconLawsEvent>(OnBodyGetLaws);
        SubscribeLocalEvent<StationAiBodyComponent, GetVerbsEvent<AlternativeVerb>>(OnBodyAlternativeVerbs);
        SubscribeLocalEvent<StationAiBodyComponent, StationAiBodyOpenUiActionEvent>(OnBodyOpenBodyUiAction);
        SubscribeLocalEvent<StationAiBodyComponent, StationAiBodyExitActionEvent>(OnBodyExitAction);

        Subs.BuiEvents<StationAiBodyControllerComponent>(StationAiBodyUiKey.Key,
            subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBodyUiOpened);
            subs.Event<StationAiBodyEnterMessage>(OnBodyUiEnterMessage);
            subs.Event<StationAiBodyExitMessage>(OnBodyUiExitMessage);
        });
    }

    private void OnBodyBoardInserted(Entity<BorgChassisComponent> chassis, ref StationAiBodyBoardInsertedEvent args)
    {
        TryInitializeBody(chassis.AsNullable(), args.Board);
    }

    private void OnBodyBoardRemoved(Entity<BorgChassisComponent> chassis, ref StationAiBodyBoardRemovedEvent args)
    {
        TryClearBodyFromRemovedBoard(chassis.AsNullable(), args.Board);
    }

    private void OnStationAiBodyControllerStartup(Entity<StationAiBodyControllerComponent> stationAi, ref ComponentStartup args)
    {
        EnsureControllerUi(stationAi);
        EnsureControllerActions(stationAi);
        UpdateBodyUiData(stationAi.AsNullable());
    }

    private void OnStationAiBodyControllerMapInit(Entity<StationAiBodyControllerComponent> stationAi, ref MapInitEvent args)
    {
        EnsureControllerUi(stationAi);
        EnsureControllerActions(stationAi);
        UpdateBodyUiData(stationAi.AsNullable());
    }

    private void OnStationAiBodyControllerShutdown(Entity<StationAiBodyControllerComponent> stationAi, ref ComponentShutdown args)
    {
        _actions.RemoveAction(stationAi.Owner, stationAi.Comp.BodyMenuAction);
        stationAi.Comp.BodyMenuAction = null;
    }

    private void OnBodyTerminating(Entity<StationAiBodyComponent> body, ref EntityTerminatingEvent args)
    {
        TryEmergencyReturnFromBody(body.AsNullable(), ejectBoard: true);
    }

    private void OnStationAiBrainGotRemovedFromCore(Entity<StationAiBodyControllerComponent> stationAi, ref EntGotRemovedFromContainerMessage args)
    {
        if (args.Container.ID != StationAiCoreComponent.Container)
            return;

        if (!HasComp<StationAiCoreComponent>(args.Container.Owner))
            return;

        if (stationAi.Comp.CurrentBody == null)
            return;

        RemComp<RelayInputMoverComponent>(stationAi.Owner);

        if (TryComp<InputMoverComponent>(stationAi.Owner, out var mover))
        {
            mover.CanMove = false;
            Dirty(stationAi.Owner, mover);
        }

        TryExitBody(stationAi.Owner);
    }

    private void OnStationAiBrainMobStateChanged(Entity<StationAiBodyControllerComponent> stationAi, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        TryExitBody(stationAi.Owner);
    }

    private void OnBodyGetLaws(Entity<StationAiBodyComponent> body, ref GetSiliconLawsEvent args)
    {
        if (args.Handled || body.Comp.LinkedAi is not { } stationAi)
            return;

        args.Laws = _siliconLaw.GetLaws(stationAi);
        args.Handled = true;
    }

    private void OnStationAiOpenBodyUiAction(Entity<StationAiBodyControllerComponent> stationAi, ref StationAiBodyOpenUiActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryOpenBodyUi(stationAi.Owner, args.Performer))
            return;

        args.Handled = true;
    }

    private void OnBodyOpenBodyUiAction(Entity<StationAiBodyComponent> body, ref StationAiBodyOpenUiActionEvent args)
    {
        if (args.Handled || body.Comp.LinkedAi is not { } stationAi)
            return;

        if (!TryOpenBodyUi(stationAi, args.Performer))
            return;

        args.Handled = true;
    }

    private void OnBodyAlternativeVerbs(Entity<StationAiBodyComponent> body, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanComplexInteract || !TryGetStationAiFromActor(args.User, out var stationAi))
            return;

        if (!CanEnterBody(stationAi, body.AsNullable(), out _, out _, out _))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("station-ai-body-enter-verb"),
            Icon = BodyEnterVerbIcon,
            Act = () => TryEnterBody(stationAi, body.AsNullable()),
        });
    }

    private void OnBodyExitAction(Entity<StationAiBodyComponent> body, ref StationAiBodyExitActionEvent args)
    {
        if (args.Handled || body.Comp.LinkedAi is not { } stationAi)
            return;

        if (!TryExitBody(stationAi))
            return;

        args.Handled = true;
    }

    private void OnBodyUiOpened(EntityUid stationAi, StationAiBodyControllerComponent controller, BoundUIOpenedEvent args)
    {
        UpdateBodyUiData((stationAi, controller));
    }

    private void OnBodyUiEnterMessage(EntityUid stationAi, StationAiBodyControllerComponent controller, StationAiBodyEnterMessage args)
    {
        if (!CanUseBodyUi((stationAi, controller), args.Actor))
            return;

        if (!TryGetEntity(args.Body, out var body))
            return;

        TryEnterBody(stationAi, body.Value);
    }

    private void OnBodyUiExitMessage(EntityUid stationAi, StationAiBodyControllerComponent controller, StationAiBodyExitMessage args)
    {
        if (!CanUseBodyUi((stationAi, controller), args.Actor))
            return;

        TryExitBody(stationAi);
    }

    /// <summary>
    /// Prepares a borg chassis as a free station AI body if the inserted entity is an AI communication board.
    /// </summary>
    public bool TryInitializeBody(Entity<BorgChassisComponent?> chassis, EntityUid board)
    {
        if (!CanInitializeBody(chassis, board))
            return false;

        DoInitializeBody(chassis.Owner, board);
        return true;
    }

    /// <summary>
    /// Returns whether the chassis can become a station AI body from the supplied board.
    /// </summary>
    public bool CanInitializeBody(Entity<BorgChassisComponent?> chassis, EntityUid board)
    {
        return Resolve(chassis.Owner, ref chassis.Comp, false) &&
               HasComp<StationAiCommunicationBoardComponent>(board);
    }

    /// <summary>
    /// Transfers a station AI brain into a prepared free body.
    /// </summary>
    public bool TryEnterBody(EntityUid stationAi, EntityUid body)
    {
        return TryEnterBody(stationAi, (body, null));
    }

    /// <summary>
    /// Transfers a station AI brain into a prepared free body.
    /// </summary>
    public bool TryEnterBody(EntityUid stationAi, Entity<StationAiBodyComponent?> body)
    {
        if (!CanEnterBody(stationAi, body, out var mindId, out var mind, out var currentBody))
            return false;

        DoEnterBody(stationAi, (body.Owner, Comp<StationAiBodyComponent>(body.Owner)), mindId, mind, currentBody);
        return true;
    }

    /// <summary>
    /// Returns whether the AI may start controlling the supplied body.
    /// </summary>
    public bool CanEnterBody(
        EntityUid stationAi,
        Entity<StationAiBodyComponent?> body,
        out EntityUid mindId,
        out MindComponent mind,
        out EntityUid? currentBody)
    {
        mindId = default;
        mind = default!;
        currentBody = null;

        if (!Resolve(body.Owner, ref body.Comp, false))
            return false;

        if (body.Comp.Board == null)
            return false;

        if (body.Comp.LinkedAi != null && body.Comp.LinkedAi != stationAi)
            return false;

        if (!_mind.TryGetMind(stationAi, out mindId, out var mindComp))
        {
            if (!TryComp<StationAiBodyControllerComponent>(stationAi, out var controller) ||
                controller.CurrentBody is not { } controlledBody ||
                !TryComp<StationAiBodyComponent>(controlledBody, out var controlledBodyComp) ||
                controlledBodyComp.LinkedAi != stationAi ||
                !_mind.TryGetMind(controlledBody, out mindId, out mindComp))
            {
                return false;
            }

            currentBody = controlledBody;
        }
        else if (TryComp<StationAiBodyControllerComponent>(stationAi, out var controller))
        {
            currentBody = controller.CurrentBody;
        }

        mind = mindComp;
        return true;
    }

    /// <summary>
    /// Returns a controlling AI brain from its current body back to the AI brain entity.
    /// </summary>
    public bool TryExitBody(EntityUid stationAi)
    {
        if (!CanExitBody(stationAi, out var body, out var bodyComp, out var mindId, out var mind, out var controller))
            return false;

        DoExitBody(stationAi, (body, bodyComp), mindId, mind, controller);
        return true;
    }

    /// <summary>
    /// Returns whether the AI is currently controlling a body and can leave it.
    /// </summary>
    public bool CanExitBody(
        EntityUid stationAi,
        out EntityUid body,
        out StationAiBodyComponent bodyComp,
        out EntityUid mindId,
        out MindComponent mind,
        out StationAiBodyControllerComponent controller)
    {
        body = default;
        bodyComp = default!;
        mindId = default;
        mind = default!;
        controller = default!;

        if (!TryComp<StationAiBodyControllerComponent>(stationAi, out var controllerComp) ||
            controllerComp.CurrentBody is not { } currentBody)
        {
            return false;
        }

        if (!TryComp<StationAiBodyComponent>(currentBody, out var currentBodyComp) ||
            currentBodyComp.LinkedAi != stationAi)
        {
            return false;
        }

        if (!_mind.TryGetMind(currentBody, out mindId, out var mindComp))
            return false;

        body = currentBody;
        bodyComp = currentBodyComp;
        mind = mindComp;
        controller = controllerComp;
        return true;
    }

    /// <summary>
    /// Returns whether the AI may select a borg chassis type for its active body.
    /// </summary>
    public bool CanSelectBodyType(
        EntityUid stationAi,
        ProtoId<BorgTypePrototype> borgType,
        out EntityUid body,
        out BorgSwitchableTypeComponent switchable)
    {
        body = default;
        switchable = default!;

        if (!TryComp<StationAiBodyControllerComponent>(stationAi, out var controller) ||
            controller.CurrentBody is not { } currentBody)
        {
            return false;
        }

        if (!TryComp<StationAiBodyComponent>(currentBody, out var bodyComp) ||
            bodyComp.LinkedAi != stationAi ||
            bodyComp.Board == null)
        {
            return false;
        }

        if (!TryComp<BorgSwitchableTypeComponent>(currentBody, out var switchableComp) ||
            !_borgSwitchableType.CanSelectBorgType((currentBody, switchableComp), borgType))
        {
            return false;
        }

        body = currentBody;
        switchable = switchableComp;
        return true;
    }

    /// <summary>
    /// Clears AI-body state from a borg chassis after its communication board is removed.
    /// </summary>
    public bool TryClearBodyFromRemovedBoard(Entity<BorgChassisComponent?> chassis, EntityUid board)
    {
        if (!CanClearBodyFromRemovedBoard(
                chassis,
                board,
                out var body,
                out var stationAi,
                out var mindId,
                out var mind,
                out var controller))
        {
            return false;
        }

        DoClearBodyFromRemovedBoard(
            (chassis.Owner, body),
            stationAi,
            mindId,
            mind,
            controller);
        return true;
    }

    /// <summary>
    /// Returns whether the removed board belongs to a prepared AI body and can clear that state.
    /// </summary>
    public bool CanClearBodyFromRemovedBoard(
        Entity<BorgChassisComponent?> chassis,
        EntityUid board,
        out StationAiBodyComponent body,
        out EntityUid? stationAi,
        out EntityUid? mindId,
        out MindComponent? mind,
        out StationAiBodyControllerComponent? controller)
    {
        body = default!;
        stationAi = null;
        mindId = null;
        mind = null;
        controller = null;

        if (!Resolve(chassis.Owner, ref chassis.Comp, false))
            return false;

        if (!TryComp<StationAiBodyComponent>(chassis.Owner, out var bodyComp) ||
            bodyComp.Board != board)
        {
            return false;
        }

        body = bodyComp;

        if (bodyComp.LinkedAi is not { } linkedAi)
            return true;

        if (!_mind.TryGetMind(chassis.Owner, out var foundMindId, out var foundMind) ||
            !TryComp<StationAiBodyControllerComponent>(linkedAi, out var controllerComp))
        {
            return false;
        }

        stationAi = linkedAi;
        mindId = foundMindId;
        mind = foundMind;
        controller = controllerComp;
        return true;
    }

    /// <summary>
    /// Returns the AI from a body that is being forcibly removed.
    /// </summary>
    public bool TryEmergencyReturnFromBody(Entity<StationAiBodyComponent?> body, bool ejectBoard)
    {
        if (!CanEmergencyReturnFromBody(body, out var stationAi, out var mindId, out var mind, out var controller))
            return false;

        DoEmergencyReturnFromBody(
            (body.Owner, Comp<StationAiBodyComponent>(body.Owner)),
            stationAi,
            mindId,
            mind,
            controller,
            ejectBoard);
        return true;
    }

    /// <summary>
    /// Returns whether a controlled body can safely hand its mind back to the AI brain.
    /// </summary>
    public bool CanEmergencyReturnFromBody(
        Entity<StationAiBodyComponent?> body,
        out EntityUid stationAi,
        out EntityUid mindId,
        out MindComponent mind,
        out StationAiBodyControllerComponent controller)
    {
        stationAi = default;
        mindId = default;
        mind = default!;
        controller = default!;

        if (!Resolve(body.Owner, ref body.Comp, false))
            return false;

        if (body.Comp.LinkedAi is not { } linkedAi || TerminatingOrDeleted(linkedAi))
            return false;

        if (!_mind.TryGetMind(body.Owner, out mindId, out var mindComp))
            return false;

        if (!TryComp<StationAiBodyControllerComponent>(linkedAi, out var controllerComp))
            return false;

        stationAi = linkedAi;
        mind = mindComp;
        controller = controllerComp;
        return true;
    }

    private void DoClearBodyFromRemovedBoard(
        Entity<StationAiBodyComponent> body,
        EntityUid? stationAi,
        EntityUid? mindId,
        MindComponent? mind,
        StationAiBodyControllerComponent? controller)
    {
        body.Comp.Board = null;
        body.Comp.LinkedAi = null;

        if (stationAi is { } ai &&
            mindId is { } aiMindId &&
            mind != null &&
            controller != null)
        {
            controller.CurrentBody = null;
            _mind.TransferTo(aiMindId, ai, mind: mind);
            Dirty(ai, controller);
        }

        if (TryComp<AccessReaderComponent>(body.Owner, out var accessReader))
            _accessReader.SetActive((body.Owner, accessReader), false);

        RemCompDeferred<StationAiBodyComponent>(body.Owner);
        UpdateAllBodyUiData();
    }

    private void DoInitializeBody(EntityUid chassis, EntityUid board)
    {
        var body = EnsureComp<StationAiBodyComponent>(chassis);

        if (body.BodyNumber == 0)
            body.BodyNumber = GetNextBodyNumber();

        body.Board = board;
        body.LinkedAi = null;

        RemComp<NameIdentifierComponent>(chassis);

        _metaData.SetEntityName(chassis, GetFreeBodyName(body.BodyNumber));
        SetFreeBodyAccess(chassis);

        Dirty(chassis, body);
        UpdateAllBodyUiData();
    }

    private void DoEnterBody(
        EntityUid stationAi,
        Entity<StationAiBodyComponent> body,
        EntityUid mindId,
        MindComponent mind,
        EntityUid? currentBody)
    {
        var controller = EnsureComp<StationAiBodyControllerComponent>(stationAi);
        var aiName = MetaData(stationAi).EntityName;

        if (currentBody != null &&
            currentBody != body.Owner &&
            TryComp<StationAiBodyComponent>(currentBody, out var currentBodyComp))
        {
            DoReleaseBody((currentBody.Value, currentBodyComp), stationAi);
        }

        body.Comp.LinkedAi = stationAi;
        controller.CurrentBody = body.Owner;
        EnsureControllerActions((stationAi, controller));

        _mind.TransferTo(mindId, body.Owner, mind: mind);
        _metaData.SetEntityName(body.Owner, aiName);
        SetControlledBodyAccess(body.Owner);
        SetStationAiRadio(stationAi, body);
        AddBodyActions(body);

        Dirty(body);
        Dirty(stationAi, controller);
        UpdateAllBodyUiData();
    }

    private void DoExitBody(
        EntityUid stationAi,
        Entity<StationAiBodyComponent> body,
        EntityUid mindId,
        MindComponent mind,
        StationAiBodyControllerComponent controller)
    {
        DoReleaseBody(body, stationAi);
        controller.CurrentBody = null;

        _mind.TransferTo(mindId, stationAi, mind: mind);

        Dirty(stationAi, controller);
        UpdateAllBodyUiData();
    }

    private void DoEmergencyReturnFromBody(
        Entity<StationAiBodyComponent> body,
        EntityUid stationAi,
        EntityUid mindId,
        MindComponent mind,
        StationAiBodyControllerComponent controller,
        bool ejectBoard)
    {
        if (ejectBoard &&
            body.Comp.Board is { } board &&
            Exists(board) &&
            !TerminatingOrDeleted(board))
        {
            _container.TryRemoveFromContainer(board, force: true);
        }

        body.Comp.LinkedAi = null;
        controller.CurrentBody = null;
        RemoveBodyActions(body);

        _mind.TransferTo(mindId, stationAi, mind: mind);

        Dirty(body);
        Dirty(stationAi, controller);
        UpdateAllBodyUiData();
    }

    private void DoReleaseBody(Entity<StationAiBodyComponent> body, EntityUid stationAi)
    {
        if (body.Comp.LinkedAi != stationAi)
            return;

        body.Comp.LinkedAi = null;
        RemoveBodyActions(body);
        _metaData.SetEntityName(body.Owner, GetFreeBodyName(body.Comp.BodyNumber));
        SetFreeBodyAccess(body.Owner);

        Dirty(body);
    }

    private void SetFreeBodyAccess(EntityUid chassis)
    {
        if (!TryComp<AccessReaderComponent>(chassis, out var accessReader))
            return;

        _accessReader.TrySetAccesses((chassis, accessReader), new List<HashSet<ProtoId<AccessLevelPrototype>>>
        {
            new() { "Captain" },
            new() { "ResearchDirector" },
            new() { "CentralCommand" },
        });
        _accessReader.SetActive((chassis, accessReader), false);
    }

    private void SetControlledBodyAccess(EntityUid chassis)
    {
        if (!TryComp<AccessReaderComponent>(chassis, out var accessReader))
            return;

        _accessReader.SetActive((chassis, accessReader), true);
    }

    private void SetStationAiRadio(EntityUid stationAi, Entity<StationAiBodyComponent> body)
    {
        if (!TryGetRadioChannelsHolderByAiCore(stationAi, out var radioChannelsHolder))
            return;

        if (TryComp<IntrinsicRadioTransmitterComponent>(body, out var transmitterReceiver)
            && TryComp<IntrinsicRadioTransmitterComponent>(radioChannelsHolder, out var transmitterTransmitter))
        {
            body.Comp.CachedChannels[nameof(IntrinsicRadioTransmitterComponent)] = [..transmitterReceiver.Channels];

            transmitterReceiver.Channels.UnionWith(transmitterTransmitter.Channels);
            Dirty(body, transmitterReceiver);
        }

        if (TryComp<ActiveRadioComponent>(body, out var activeRadioReceiver)
            && TryComp<ActiveRadioComponent>(radioChannelsHolder, out var activeRadioTransmitter))
        {
            body.Comp.CachedChannels[nameof(ActiveRadioComponent)] = [..activeRadioReceiver.Channels];

            activeRadioReceiver.Channels.UnionWith(activeRadioTransmitter.Channels);
            Dirty(body, activeRadioReceiver);
        }

        Dirty(body);
    }

    private bool TryGetRadioChannelsHolderByAiCore(EntityUid stationAi, [NotNullWhen(true)] out EntityUid? radioChannelsHolder)
    {
        radioChannelsHolder = null;
        if (!TryComp<ContainerCompComponent>(stationAi, out var containerComp))
            return false;

        if (!_container.TryGetContainer(stationAi, containerComp.Container, out var container))
            return false;

        foreach (var containedEntity in container.ContainedEntities)
        {
            var proto = Prototype(containedEntity);
            if (proto == null || proto != containerComp.Proto)
                continue;

            radioChannelsHolder = containedEntity;
            return true;
        }

        return false;
    }

    private void AddBodyActions(Entity<StationAiBodyComponent> body)
    {
        _actions.AddAction(body.Owner, ref body.Comp.BodyMenuAction, BodyMenuAction);
        _actions.AddAction(body.Owner, ref body.Comp.BodyExitAction, BodyExitAction);
    }

    private void RemoveBodyActions(Entity<StationAiBodyComponent> body)
    {
        _actions.RemoveAction(body.Owner, body.Comp.BodyMenuAction);
        body.Comp.BodyMenuAction = null;

        _actions.RemoveAction(body.Owner, body.Comp.BodyExitAction);
        body.Comp.BodyExitAction = null;
    }

    private void EnsureControllerActions(Entity<StationAiBodyControllerComponent> stationAi)
    {
        _actions.AddAction(stationAi.Owner, ref stationAi.Comp.BodyMenuAction, BodyMenuAction);
        Dirty(stationAi.Owner, stationAi.Comp);
    }

    private void EnsureControllerUi(EntityUid stationAi)
    {
        _ui.SetUi(
            (stationAi, null),
            StationAiBodyUiKey.Key,
            new InterfaceData(BodyUiClientType, interactionRange: -1f, requireInputValidation: false));
    }

    private void UpdateBodyUiData(Entity<StationAiBodyControllerComponent?> stationAi)
    {
        if (!Resolve(stationAi.Owner, ref stationAi.Comp, false))
            return;

        stationAi.Comp.Bodies = BuildBodyEntries(GetCurrentBody(stationAi.Owner));
        Dirty(stationAi.Owner, stationAi.Comp);
    }

    private void UpdateAllBodyUiData()
    {
        var query = EntityQueryEnumerator<StationAiBodyControllerComponent>();

        while (query.MoveNext(out var stationAi, out var controller))
        {
            UpdateBodyUiData((stationAi, controller));
        }
    }

    private List<StationAiBodyEntry> BuildBodyEntries(EntityUid? currentBody)
    {
        var bodies = new List<StationAiBodyEntry>();
        var query = EntityQueryEnumerator<StationAiBodyComponent, MetaDataComponent>();

        while (query.MoveNext(out var bodyUid, out var body, out var meta))
        {
            if (body.Board == null)
                continue;

            NetEntity? linkedAi = body.LinkedAi is { } ai ? GetNetEntity(ai) : null;
            bodies.Add(new StationAiBodyEntry(
                GetNetEntity(bodyUid),
                body.BodyNumber,
                meta.EntityName,
                linkedAi,
                currentBody == bodyUid));
        }

        bodies.Sort((left, right) => left.BodyNumber.CompareTo(right.BodyNumber));
        return bodies;
    }

    private bool TryOpenBodyUi(EntityUid stationAi, EntityUid actor)
    {
        EnsureControllerUi(stationAi);

        if (!TryComp<StationAiBodyControllerComponent>(stationAi, out var controller) ||
            !CanUseBodyUi((stationAi, controller), actor))
        {
            return false;
        }

        UpdateBodyUiData((stationAi, controller));
        return _ui.TryOpenUi((stationAi, null), StationAiBodyUiKey.Key, actor);
    }

    private bool CanUseBodyUi(Entity<StationAiBodyControllerComponent> stationAi, EntityUid actor)
    {
        if (actor == stationAi.Owner)
            return true;

        return stationAi.Comp.CurrentBody == actor &&
               TryComp<StationAiBodyComponent>(actor, out var body) &&
               body.LinkedAi == stationAi.Owner;
    }

    private bool TryGetStationAiFromActor(EntityUid actor, out EntityUid stationAi)
    {
        if (HasComp<StationAiBodyControllerComponent>(actor))
        {
            stationAi = actor;
            return true;
        }

        if (TryComp<StationAiBodyComponent>(actor, out var body) &&
            body.LinkedAi is { } linkedAi &&
            HasComp<StationAiBodyControllerComponent>(linkedAi))
        {
            stationAi = linkedAi;
            return true;
        }

        stationAi = default;
        return false;
    }

    private EntityUid? GetCurrentBody(EntityUid stationAi)
    {
        if (!TryComp<StationAiBodyControllerComponent>(stationAi, out var controller) ||
            controller.CurrentBody is not { } currentBody ||
            !Exists(currentBody))
        {
            return null;
        }

        return currentBody;
    }

    private string GetFreeBodyName(int bodyNumber)
    {
        return Loc.GetString("station-ai-body-name", ("number", bodyNumber));
    }

    private int GetNextBodyNumber()
    {
        var number = 0;
        var query = EntityQueryEnumerator<StationAiBodyComponent>();

        while (query.MoveNext(out _, out var body))
        {
            if (body.BodyNumber > number)
                number = body.BodyNumber;
        }

        return number + 1;
    }
}
