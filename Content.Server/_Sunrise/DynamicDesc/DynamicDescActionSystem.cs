using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared.Actions;
using Content.Server.Popups;
using Content.Shared.Mind.Components;
using Content.Shared.DynamicDesc;
using Robust.Shared.Player;

namespace Content.Server.DynamicDesc;

public sealed class DynamicDescChangeActionSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    private const int MaxDynamicDescLength = 100;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DynamicDescComponent, DynamicDescChangeEvent>(OnDynamicDescChange);
        SubscribeLocalEvent<DynamicDescComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DynamicDescComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<DynamicDescComponent, GetItemActionsEvent>(OnGetActions);
    }

    private void OnMapInit(EntityUid uid, DynamicDescComponent component, MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref component.DynamicDescChangeActionEntity, component.DynamicDescChangeAction);
        Dirty(uid, component);

    }

    private void OnGetActions(EntityUid uid, DynamicDescComponent component, GetItemActionsEvent args)
    {
        args.AddAction(component.DynamicDescChangeActionEntity);
    }

    private void OnMindAdded(EntityUid uid, DynamicDescComponent component, MindAddedMessage args)
    {
        _actions.AddAction(uid, ref component.DynamicDescChangeActionEntity, component.DynamicDescChangeAction);
    }

    private void OnDynamicDescChange(EntityUid uid, DynamicDescComponent component, DynamicDescChangeEvent args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        if (!TryComp(uid, out DynamicDescComponent? comp))
            return;

        _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString("action-name-dynamic-description-change"), "",
            (string dynamicdesc) =>
            {
                if (dynamicdesc.Length > MaxDynamicDescLength)
                {
                    return;
                } else
                {
                    comp!.Content = dynamicdesc;
                    _popupSystem.PopupEntity(Loc.GetString("action-dynamic-description-change-popup"), uid);
                }
            });

        args.Handled = true;
    }
}
