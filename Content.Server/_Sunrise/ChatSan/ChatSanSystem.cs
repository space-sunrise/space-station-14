using System.Text.RegularExpressions;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Chat;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server._Sunrise.ChatSan;

/// <summary>
/// Санитизация чата
/// </summary>
public sealed class ChatSanSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ISharedChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private bool _enabled;
    private bool _aggressive;

    /// <summary>
    /// Этот реджекс паттерн используется для поиска ссылок в сообщении. Он очень расширенный, тобишь даже test:\\example.com
    /// он будет отфильтровывать.
    /// </summary>
    private static readonly Regex UrlRegex = new("[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&\\/\\/=]*)");

    /// <summary>
    /// Этот реджекс используется для поиска ASCII артов в сообщении. По сути он выбирает все символы, которые нельзя
    /// напечатать на клавиатуре. Я проверил, что благодаря ему можно напечатать все локали.
    /// </summary>
    private static readonly Regex AsciiArtRegex = new("[^\\x09\\x0A\\x0D\\x20-\\x7E\\u0400-\\u04FF]");

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

    /// <summary>
    /// Обрабатывает реквест к системе автоматической санитизации чата.
    /// </summary>
    /// <param name="ev"></param>
    private void HandleChatSanRequest(ref ChatSanRequestEvent ev)
    {
        if (!_enabled)
            return;

        if (ev.Handled)
            return;
        ev.Handled = true;

        // Мы используем эту систему исключительно если цель контроллирует игрок.
        if (!_playerManager.TryGetSessionByEntity(ev.Source, out var session))
            return;


        switch (_aggressive)
        {
            // Агрессивный режим: блокируем сообщение от юзера если нашли недопустимую последовательность.
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
                    _chat.SendAdminAlert(Loc.GetString(
                        "chatsan-admin-alert",
                        ("user", session.Data.UserName),
                        ("message_cropped", ev.Message.Length > 20
                            ? ev.Message.Substring(0, 20)
                            : ev.Message)));
                }
                return;
            // Щадящий режим: удаляем все недопустимые последовательности.
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
