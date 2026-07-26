using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Shared._Sunrise.CollectiveMind;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.CollectiveMind;

public sealed class CollectiveMindActionSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly CollectiveMindSystem _collectiveMind = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CollectiveMindSendActionEvent>(OnSendAction);
    }

    private void OnSendAction(CollectiveMindSendActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryOpenDialog(
            args.Performer,
            args.DialogTitle,
            args.DialogPrompt,
            args.CollectiveMind);
    }

    public bool TryOpenDialog(EntityUid performer, LocId dialogTitle, LocId dialogPrompt,
        ProtoId<CollectiveMindPrototype>? requestedMind = null)
    {
        if (!CanOpenDialog(performer, requestedMind, out var actor, out var mind))
            return false;

        OpenDialog(actor, mind, dialogTitle, dialogPrompt);
        return true;
    }

    public bool CanOpenDialog(EntityUid performer, ProtoId<CollectiveMindPrototype>? requestedMind,
        out Entity<ActorComponent> actor, out ProtoId<CollectiveMindPrototype> mind)
    {
        actor = default;
        mind = default;
        if (!TryComp<ActorComponent>(performer, out var actorComponent))
            return false;

        if (requestedMind is { } explicitMind)
            mind = explicitMind;
        else if (!_collectiveMind.TryGetDefaultMind(performer, out mind))
            return false;

        if (!_collectiveMind.TryResolveSender(performer, mind, out _))
            return false;

        actor = (performer, actorComponent);
        return true;
    }

    private void OpenDialog(Entity<ActorComponent> performer, ProtoId<CollectiveMindPrototype> mind,
        LocId dialogTitle, LocId dialogPrompt)
    {
        var session = performer.Comp.PlayerSession;
        var prototype = _prototype.Index(mind);
        var title = Loc.GetString(dialogTitle, ("channel", prototype.LocalizedName));

        _quickDialog.OpenDialog<string>(
            session,
            title,
            Loc.GetString(dialogPrompt),
            message => TrySendMessage(performer, session, mind, message));
    }

    private bool TrySendMessage(EntityUid performer, ICommonSession session, ProtoId<CollectiveMindPrototype> mind, string message)
    {
        // Ответ на QuickDialog приходит позже Action, поэтому заново проверяем исполнителя.
        if (Deleted(performer) || session.AttachedEntity != performer)
            return false;

        return _chat.TrySendCollectiveMindMessage(
            performer,
            message,
            mind,
            player: session
            );
    }
}
