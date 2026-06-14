using Content.Shared._Sunrise.Silicons.StationAi;
using Content.Shared.Actions;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.Silicons.StationAi;

public sealed partial class StationAiBodySystem
{
    /*
     * Interface partial.
     *
     * This file owns player-facing controls for station AI bodies:
     * action buttons, the alternative verb for entering a body, bound UI messages,
     * and the body list sent to the AI body selection interface.
     */

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private const string BodyUiClientType = "StationAiBodyBoundUserInterface";

    private static readonly SpriteSpecifier BodyEnterVerbIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png"));

    #region Initialize

    private void InitializeBodyInterface()
    {
        SubscribeLocalEvent<StationAiBodyControllerComponent, ComponentStartup>(OnStationAiBodyControllerStartup);
        SubscribeLocalEvent<StationAiBodyControllerComponent, MapInitEvent>(OnStationAiBodyControllerMapInit);
        SubscribeLocalEvent<StationAiBodyControllerComponent, ComponentShutdown>(OnStationAiBodyControllerShutdown);
        SubscribeLocalEvent<StationAiBodyControllerComponent, StationAiBodyOpenUiActionEvent>(OnStationAiOpenBodyUiAction);
        SubscribeLocalEvent<StationAiBodyComponent, GetVerbsEvent<AlternativeVerb>>(OnBodyAlternativeVerbs);
        SubscribeLocalEvent<StationAiBodyComponent, StationAiBodyOpenUiActionEvent>(OnBodyOpenBodyUiAction);

        Subs.BuiEvents<StationAiBodyControllerComponent>(StationAiBodyUiKey.Key,
            subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBodyUiOpened);
            subs.Event<StationAiBodyEnterMessage>(OnBodyUiEnterMessage);
            subs.Event<StationAiBodyExitMessage>(OnBodyUiExitMessage);
        });
    }

    #endregion

    #region Events

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

    #endregion

    #region Actions

    private void GrantControlledBodyActions(Entity<StationAiBodyComponent> body)
    {
        RevokeControlledBodyActions(body);

        foreach (var action in body.Comp.ControlledBodyActions)
        {
            EntityUid? actionEnt = null;
            _actions.AddAction(body.Owner, ref actionEnt, action);

            if (actionEnt != null)
                body.Comp.ControlledBodyActionEntities.Add(actionEnt.Value);
        }
    }

    private void RevokeControlledBodyActions(Entity<StationAiBodyComponent> body)
    {
        foreach (var actionEnt in body.Comp.ControlledBodyActionEntities)
        {
            _actions.RemoveAction(body.Owner, actionEnt);
        }

        body.Comp.ControlledBodyActionEntities.Clear();
    }

    private void EnsureControllerActions(Entity<StationAiBodyControllerComponent> stationAi)
    {
        _actions.AddAction(stationAi.Owner, ref stationAi.Comp.BodyMenuAction, stationAi.Comp.BodyMenuActionPrototype);
        Dirty(stationAi.Owner, stationAi.Comp);
    }

    #endregion

    #region UI Data

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

    #endregion

    #region Helpers

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

    #endregion
}
