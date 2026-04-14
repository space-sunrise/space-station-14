using System.Collections.Generic;
using System.Text.RegularExpressions;
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

    private static readonly HashSet<string> AllowedTags = new()
    {
        "bold",
        "italic",
        "bolditalic",
        "bullet",
        "color",
        "heading",
        "mono",
        "head"
    };

    private static readonly Regex TagRegex = new(@"\[(/?)([^]]+)\]", RegexOptions.Compiled);

    private bool _isEnabled;
    private int _maxEmoteLength;
    private float _maxEmoteDistance;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(SunriseCCVars.DirectionalEmoteEnabled, value => _isEnabled = value, true);
        _cfg.OnValueChanged(SunriseCCVars.DirectionalEmoteMaxLength, value => _maxEmoteLength = value, true);
        _cfg.OnValueChanged(SunriseCCVars.DirectionalEmoteMaxDistance, value => _maxEmoteDistance = value, true);

        SubscribeNetworkEvent<DirectionalEmoteAttemptEvent>(HandleDirectionalEmoteAttemptEvent);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(SunriseCCVars.DirectionalEmoteEnabled, value => _isEnabled = value);
        _cfg.UnsubValueChanged(SunriseCCVars.DirectionalEmoteMaxLength, value => _maxEmoteLength = value);
        _cfg.UnsubValueChanged(SunriseCCVars.DirectionalEmoteMaxDistance, value => _maxEmoteDistance = value);
    }

    private void HandleDirectionalEmoteAttemptEvent(DirectionalEmoteAttemptEvent args, EntitySessionEventArgs eventArgs)
    {
        if (!_isEnabled)
            return;

        if (eventArgs.SenderSession.AttachedEntity == null)
            return;

        var source = eventArgs.SenderSession.AttachedEntity.Value;
        var target = GetEntity(args.Target);
        var filteredText = FilterTags(args.Text);

        if (!IsValid(filteredText, source, target, args.HideName))
            return;

        var wrappedMessage = args.HideName
            ? filteredText
            : Loc.GetString("directional-emote-wrap-message", ("source", MetaData(source).EntityName), ("message", filteredText));

        if (!TryComp<ActorComponent>(source, out var sourceActor) || !TryComp<ActorComponent>(target, out var targetActor))
            return;

        if (!TryComp<DirectionalEmoteComponent>(source, out var sourceEmote) || !TryComp<DirectionalEmoteComponent>(target, out var targetEmote))
            return;

        _chatManager.ChatMessageToMany(ChatChannel.Emotes, filteredText, wrappedMessage, source, false, true, [targetActor.PlayerSession.Channel, sourceActor.PlayerSession.Channel]);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(source):source} send directional emote to {ToPrettyString(target):target}: {filteredText}");

        sourceEmote.LastSendAt = _timing.CurTime;
        sourceEmote.LastEmote = filteredText;
        Dirty(source, sourceEmote);
    }

    private bool IsValid(string text, EntityUid source, EntityUid target, bool hideName)
    {
        if (!TryComp<DirectionalEmoteComponent>(source, out var sourceEmote) ||
            !TryComp<DirectionalEmoteComponent>(target, out var targetEmote))
            return false;

        if (!sourceEmote.CanSendEmotes || !targetEmote.CanReceiveEmotes)
            return false;

        if (hideName && !sourceEmote.CanHideName)
            return false;

        if (sourceEmote.LastSendAt + sourceEmote.Cooldown > _timing.CurTime)
            return false;

        if (!_examineSystem.InRangeUnOccluded(source, target, _maxEmoteDistance))
            return false;

        if (text.Length > _maxEmoteLength || string.IsNullOrWhiteSpace(text))
            return false;

        return true;
    }

    private static string FilterTags(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return TagRegex.Replace(text, match =>
        {
            var rawTag = match.Groups[2].Value;
            var tagName = rawTag.Split(new[] { ' ', '=' }, 2)[0].ToLowerInvariant();

            return AllowedTags.Contains(tagName) ? match.Value : string.Empty;
        });
    }
}
