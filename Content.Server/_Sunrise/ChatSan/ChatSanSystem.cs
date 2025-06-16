using System.Text.RegularExpressions;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Chat;
using Robust.Shared.Configuration;

namespace Content.Server._Sunrise.ChatSan;

/// <summary>
/// Санитизация чата
/// </summary>
public sealed class ChatSanSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ISharedChatManager _chat = default!;

    private bool _enabled;
    private bool _aggressive;

    private static readonly Regex UrlRegex = new("[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&\\/\\/=]*)");
    private static readonly Regex AsciiArtRegex = new("[^\\w\\s!?.,@#$%^&*~|\\(\\)\\[\\]\\{\\}\\/-]");

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _enabled = _configurationManager.GetCVar(SunriseCCVars.ChatSanitizationEnable);
        _aggressive = _configurationManager.GetCVar(SunriseCCVars.ChatSanitizationAggressive);

        _configurationManager.OnValueChanged(SunriseCCVars.ChatSanitizationEnable, obj => { _enabled = obj; });
        _configurationManager.OnValueChanged(SunriseCCVars.ChatSanitizationAggressive, obj => { _aggressive = obj; });

        SubscribeLocalEvent<ChatSanRequestEvent>(HandleChatSanRequest);
    }

    private void HandleChatSanRequest(ref ChatSanRequestEvent ev)
    {
        if (!_enabled)
            return;

        if (ev.Handled)
            return;
        ev.Handled = true;

        // 1. Если regex нашел url: [-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&\/\/=]*)
        switch (_aggressive)
        {
            case true:
                var conditions = new List<bool>
                {
                    IsUrlFound(ev.Message),
                    IsAsciiArtFound(ev.Message),
                };
                var cancelled = conditions.Contains(true);
                ev.Cancelled = cancelled;
                if (cancelled)
                {
                    _chat.SendAdminAlert(Loc.GetString("chatsan-admin-alert", ("message_cropped", ev.Message.Substring(0, 20))));
                }
                return;
            case false:
                ev.Message = UrlReplace(ev.Message);
                ev.Message = AsciiArtReplace(ev.Message);
                break;
        }

    }

    # region Handlers

    // 1. Если regex нашел url: [-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&\/\/=]*)
    private bool IsUrlFound(string message)
    {
        return UrlRegex.IsMatch(message);
    }

    private string UrlReplace(string message, string replaceTo = "")
    {
        return UrlRegex.Replace(message, replaceTo);
    }

    // 2. Если regex нашел символы кроме разрешенного списка: [^\w\s!?.,@#$%^&*~|\(\)\[\]\{\}\/-]
    private bool IsAsciiArtFound(string message)
    {
        return AsciiArtRegex.IsMatch(message);
    }

    private string AsciiArtReplace(string message, string replaceTo = "")
    {
        return AsciiArtRegex.Replace(message, replaceTo);
    }

    # endregion
}
