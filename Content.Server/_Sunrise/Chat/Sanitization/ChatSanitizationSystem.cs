using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Chat;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.Chat.Sanitization;

/// <summary>
/// Система автоматической очистки (санитизации) чата.
/// </summary>
public sealed partial class ChatSanitizationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly ISharedChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private bool _chatSanitizationEnabled;
    private bool _aggressive;

    /// <summary>
    /// Расширенный паттерн для обнаружения ссылок.
    /// </summary>
    private static readonly Regex UrlRegex =
        new("[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&\\/\\/=]*)", RegexOptions.Compiled);

    /// <summary>
    /// ASCII-art фильтр: отбрасывает нестандартные символы, но сохраняет все локали и управляемые символы.
    /// </summary>
    private static readonly Regex AsciiArtRegex =
        new("[^\\x09\\x0A\\x0D\\x20-\\x7E\\u0400-\\u04FF]", RegexOptions.Compiled);

    private const int MaxAdminAlertMessageLength = 20;

    public override void Initialize()
    {
        base.Initialize();

        _chatSanitizationEnabled = _configuration.GetCVar(SunriseCCVars.ChatSanitizationEnable);
        _aggressive = _configuration.GetCVar(SunriseCCVars.ChatSanitizationAggressive);

        _configuration.OnValueChanged(SunriseCCVars.ChatSanitizationEnable, val => _chatSanitizationEnabled = val);
        _configuration.OnValueChanged(SunriseCCVars.ChatSanitizationAggressive, val => _aggressive = val);

        SubscribeLocalEvent<ActorComponent, TrySendChatMessageEvent>(OnTrySendChatMessage);

        InitializeAntiSpam();
    }

    private void OnTrySendChatMessage(Entity<ActorComponent> ent, ref TrySendChatMessageEvent args)
    {
        if (args.Cancelled)
            return;

        ApplySanitization(ent, ref args);

        if (args.Cancelled)
            return;

        SpamCheck(ent, ref args);
    }

    private void ApplySanitization(Entity<ActorComponent> ent, ref TrySendChatMessageEvent args)
    {
        if (!_chatSanitizationEnabled)
            return;

        if (!_player.TryGetSessionByEntity(ent, out var session))
            return;

        if (_aggressive)
            HandleAggressiveSanitization(session, ref args);
        else
            HandleSoftSanitization(ref args);
    }

    private void HandleAggressiveSanitization(ICommonSession session, ref TrySendChatMessageEvent args)
    {
        if (!ContainsProhibitedContent(args.Message, out var reason))
            return;

        NotifyAdminsAboutBlockedMessage(session, args.Message, reason);
        args.Cancel();
    }

    private static void HandleSoftSanitization(ref TrySendChatMessageEvent args)
    {
        args.Message = UrlRegex.Replace(args.Message, string.Empty);
        args.Message = AsciiArtRegex.Replace(args.Message, string.Empty);
    }

    private bool ContainsProhibitedContent(string message, [NotNullWhen(true)] out string? reason)
    {
        reason = null;

        if (UrlRegex.IsMatch(message))
        {
            reason = Loc.GetString("chatsan-blocked-reason-url");
            return true;
        }

        if (AsciiArtRegex.IsMatch(message))
        {
            reason = Loc.GetString("chatsan-blocked-reason-ascii-art");
            return true;
        }

        return false;
    }

    private void NotifyAdminsAboutBlockedMessage(ICommonSession session, string message, string reason)
    {
        var cropped = message.Length > MaxAdminAlertMessageLength
            ? message[..MaxAdminAlertMessageLength]
            : message;

        _chat.SendAdminAlert(Loc.GetString(
            "chatsan-admin-alert",
            ("user", session.Data.UserName),
            ("reason", reason),
            ("message_cropped", cropped)
        ));
    }
}
