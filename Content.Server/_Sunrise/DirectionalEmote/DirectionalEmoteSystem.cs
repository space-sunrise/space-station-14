using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared._Sunrise.DirectionalEmote;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Examine;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.DirectionalEmote;

public sealed partial class DirectionalEmoteSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private int _maxEmoteLength;
    private float _maxEmoteDistance;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(SunriseCCVars.DirectionalEmoteMaxLength, value => _maxEmoteLength = value, true);
        _cfg.OnValueChanged(SunriseCCVars.DirectionalEmoteMaxDistance, value => _maxEmoteDistance = value, true);

        SubscribeNetworkEvent<DirectionalEmoteAttemptEvent>(HandleDirectionalEmoteAttemptEvent);
    }

    private void HandleDirectionalEmoteAttemptEvent(DirectionalEmoteAttemptEvent args, EntitySessionEventArgs eventArgs)
    {
        if (eventArgs.SenderSession.AttachedEntity == null)
            return;

        var source = eventArgs.SenderSession.AttachedEntity.Value;
        var target = GetEntity(args.Target);

        if (!IsValid(args, source, target))
            return;

        var wrappedMessage = args.HideName
            ? args.Text
            : Loc.GetString("directional-emote-wrap-message", ("source", MetaData(source).EntityName), ("message", args.Text));

        if (!TryComp<ActorComponent>(source, out var sourceActor) || !TryComp<ActorComponent>(target, out var targetActor))
            return;

        if (!TryComp<DirectionalEmoteComponent>(source, out var sourceEmote) || !TryComp<DirectionalEmoteComponent>(target, out var targetEmote))
            return;

        _chatManager.ChatMessageToMany(ChatChannel.Emotes, args.Text, wrappedMessage, source, false, true, [targetActor.PlayerSession.Channel, sourceActor.PlayerSession.Channel]);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(source):source} send directional emote to {ToPrettyString(target):target}: {args.Text}");

        sourceEmote.LastSendAt = _timing.CurTime;
        sourceEmote.LastEmote = args.Text;
        Dirty(source, sourceEmote);
    }

    private bool IsValid(DirectionalEmoteAttemptEvent args, EntityUid source, EntityUid target)
    {
        if (!TryComp<DirectionalEmoteComponent>(source, out var sourceEmote) ||
            !TryComp<DirectionalEmoteComponent>(target, out var targetEmote))
            return false;

        if (!sourceEmote.CanSendEmotes || !targetEmote.CanReceiveEmotes)
            return false;

        if (args.HideName && !sourceEmote.CanHideName)
            return false;

        if (sourceEmote.LastSendAt + sourceEmote.Cooldown > _timing.CurTime)
            return false;

        if (!_examineSystem.InRangeUnOccluded(source, target, _maxEmoteDistance))
            return false;

        if (args.Text.Length > _maxEmoteLength || string.IsNullOrWhiteSpace(args.Text))
            return false;

        return true;
    }
}
