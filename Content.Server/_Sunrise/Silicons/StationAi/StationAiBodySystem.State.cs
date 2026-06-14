using Content.Server.Silicons.Borgs;
using Content.Shared._Sunrise.Silicons.StationAi;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Movement.Components;
using Content.Shared.NameIdentifier;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Silicons.StationAi;

public sealed partial class StationAiBodySystem
{
    /*
     * State partial.
     *
     * This file owns the gameplay state machine of station AI bodies:
     * preparing a borg chassis as a free AI body, entering and leaving a body,
     * recovering the AI during body deletion or board removal, and transferring MindComponent
     * between the AI brain entity and the controlled body.
     */

    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly BorgSwitchableTypeSystem _borgSwitchableType = default!;

    #region Initialize

    private void InitializeBodyState()
    {
        SubscribeLocalEvent<BorgChassisComponent, StationAiBodyBoardInsertedEvent>(OnBodyBoardInserted);
        SubscribeLocalEvent<BorgChassisComponent, StationAiBodyBoardRemovedEvent>(OnBodyBoardRemoved);
        SubscribeLocalEvent<StationAiBodyControllerComponent, EntGotRemovedFromContainerMessage>(OnStationAiBrainGotRemovedFromCore);
        SubscribeLocalEvent<StationAiBodyControllerComponent, MobStateChangedEvent>(OnStationAiBrainMobStateChanged);
        SubscribeLocalEvent<StationAiBodyComponent, EntityTerminatingEvent>(OnBodyTerminating);
        SubscribeLocalEvent<StationAiBodyComponent, StationAiBodyExitActionEvent>(OnBodyExitAction);
    }

    #endregion

    #region Events

    private void OnBodyBoardInserted(Entity<BorgChassisComponent> chassis, ref StationAiBodyBoardInsertedEvent args)
    {
        TryInitializeBody(chassis.AsNullable(), args.Board);
    }

    private void OnBodyBoardRemoved(Entity<BorgChassisComponent> chassis, ref StationAiBodyBoardRemovedEvent args)
    {
        TryClearBodyFromRemovedBoard(chassis.AsNullable(), args.Board);
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

    private void OnBodyExitAction(Entity<StationAiBodyComponent> body, ref StationAiBodyExitActionEvent args)
    {
        if (args.Handled || body.Comp.LinkedAi is not { } stationAi)
            return;

        if (!TryExitBody(stationAi))
            return;

        args.Handled = true;
    }

    #endregion

    #region Public API

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

    #endregion

    #region State Changes

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

        RevokeControlledBodyActions(body);
        RevokeStationAiRadioChannels(body);

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
        GrantStationAiRadioChannels(stationAi, body);
        GrantControlledBodyActions(body);

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
        RevokeControlledBodyActions(body);
        RevokeStationAiRadioChannels(body);

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
        RevokeControlledBodyActions(body);
        RevokeStationAiRadioChannels(body);
        _metaData.SetEntityName(body.Owner, GetFreeBodyName(body.Comp.BodyNumber));

        Dirty(body);
    }

    #endregion

    #region Helpers

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

    #endregion
}
