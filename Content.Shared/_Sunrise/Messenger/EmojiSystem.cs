using System.Collections.Frozen;
using System.Text.RegularExpressions;
using Content.Shared.Chat;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Messenger;

/// <summary>
/// Система для работы с эмодзи в мессенджере
/// </summary>
public abstract class SharedEmojiSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private const ChatChannel EmojiSupportedChannels =
        ChatChannel.OOC
        | ChatChannel.LOOC
        | ChatChannel.Dead
        | ChatChannel.AdminRelated
        | ChatChannel.Server;

    public FrozenDictionary<string, EmojiPrototype> Emojis { get; private set; } =
        FrozenDictionary<string, EmojiPrototype>.Empty;

    private static readonly Regex EmojiRegex = new(
        @"(?<![a-zA-Z0-9_]):[^\s:]+:(?![a-zA-Z0-9_])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);

        CollectEmojis();
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        CollectEmojis();
    }

    /// <summary>
    /// Парсит текст сообщения и заменяет коды эмодзи на их представление в формате для RichTextLabel
    /// </summary>
    public string ParseEmojis(string text)
    {
        return EmojiRegex.Replace(text, match =>
        {
            if (Emojis.TryGetValue(match.Value, out var emoji))
                return $"[emoji id=\"{emoji.ID}\"]";

            return match.Value;
        });
    }

    /// <summary>
    /// Фильтрует/заменяет коды эмодзи в тексте, к которым у пользователя нет доступа.
    /// </summary>
    public string FilterBlockedEmojis(string text, NetUserId userId, ISharedSponsorsManager? sponsorsManager)
    {
        if (!IsContainsAnyEmoji(text))
            return text;

        return EmojiRegex.Replace(text, match =>
        {
            if (Emojis.TryGetValue(match.Value, out var emoji))
            {
                if (emoji.SponsorOnly)
                {
                    var hasAccess = false;
                    if (sponsorsManager != null)
                    {
                        if (sponsorsManager.TryGetPrototypes(userId, out var prototypes))
                        {
                            hasAccess = prototypes.Contains(emoji.ID);
                        }
                    }

                    if (!hasAccess)
                    {
                        return $":\u200b{match.Value.Trim(':')}\u200b:";
                    }
                }
            }

            return match.Value;
        });
    }

    private void CollectEmojis()
    {
        Emojis = _prototype.EnumeratePrototypes<EmojiPrototype>()
            .ToFrozenDictionary(e => e.Code, e => e);
    }

    /// <summary>
    /// Проверяет, есть ли в строке ВОЗМОЖНЫЙ эмодзи.
    /// Это быстрая проверка для early return,
    /// чтобы не делать более сложную и точную проверку, если в строке нет ни одного эмодзи
    /// </summary>
    /// <param name="text">Текст для проверки</param>
    /// <returns>Есть в строке возможный эмодзи или нет</returns>
    /// <remarks>
    /// Это чуть быстрее, чем регекс, я думаю тут это важно
    /// </remarks>
    public static bool IsContainsAnyEmoji(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length < 3)
            return false;

        var firstColon = text.IndexOf(':');
        if (firstColon == -1)
            return false; // Нет ни одного двоеточия

        var lastColon = text.LastIndexOf(':');

        // Вернет true, если двоеточий больше одного и между ними есть хотя бы 1 символ
        return lastColon > firstColon + 1;
    }

    public static bool IsEmojiAllowedInChannel(ChatChannel channel)
    {
        return channel == ChatChannel.None || (channel & EmojiSupportedChannels) != 0;
    }

    public bool IsEmojiAllowedForPlayer(string emojiId, NetUserId userId, ISharedSponsorsManager? sponsorsManager)
    {
        if (!_prototype.TryIndex<EmojiPrototype>(emojiId, out var emoji))
            return false;

        if (!emoji.SponsorOnly)
            return true;

        if (sponsorsManager == null)
            return false;

        if (sponsorsManager.TryGetPrototypes(userId, out var prototypes))
        {
            return prototypes.Contains(emojiId);
        }

        return false;
    }
}
