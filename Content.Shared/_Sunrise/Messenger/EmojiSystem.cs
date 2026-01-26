using System.Text.RegularExpressions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Messenger;

/// <summary>
/// Система для работы с эмодзи в мессенджере
/// </summary>
public abstract class EmojiSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;

    /// <summary>
    /// Парсит текст сообщения и заменяет коды эмодзи на их представление в формате для RichTextLabel
    /// </summary>
    public string ParseEmojis(string text)
    {
        foreach (var emoji in PrototypeManager.EnumeratePrototypes<EmojiPrototype>())
        {
            var escapedCode = Regex.Escape(emoji.Code);
            var pattern = $@"(?<![a-zA-Z0-9_]){escapedCode}(?![a-zA-Z0-9_])";
            var replacement = $@"[emoji id=""{emoji.ID}""]";
            text = Regex.Replace(text, pattern, replacement, RegexOptions.IgnoreCase);
        }

        return text;
    }

    /// <summary>
    /// Получает все эмодзи
    /// </summary>
    public IEnumerable<EmojiPrototype> GetAllEmojis()
    {
        return PrototypeManager.EnumeratePrototypes<EmojiPrototype>();
    }
}
