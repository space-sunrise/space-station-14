using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.UserInterface.Controls;

public sealed class RichTextButton : Button
{
    private static readonly Type[] TagsAllowed =
    [
        typeof(ColorTag),
        typeof(UserInterface.RichText.TextureTag),
    ];

    public new RichTextLabel Label { get; }

    public RichTextButton()
    {
        Label = new RichTextLabel();

        AddChild(Label);
    }

    // TODO: Мб вынести эти два метода как экстеншен класс для строки/цвета.
    // Или перенести куда, где это можно будет использовать извне
    /// <summary>
    /// Изменяет цвет текста, чтобы он был читаем на фоне кнопки
    /// </summary>
    private static string MakeTextReadable(string text, Color backgroundColor)
    {
        var newTextColor = GetReadableTextColor(backgroundColor);
        var colorizedText = $"[color={newTextColor}]{text}[/color]";

        return colorizedText;
    }

    /// <summary>
    /// Возвращает белый или чёрный цвет, обеспечивающий читаемость на заданном фоне.
    /// </summary>
    private static string GetReadableTextColor(Color background)
    {
        // Вычисляем относительную яркость
        var brightness = 0.299 * background.R +
                         0.587 * background.G +
                         0.114 * background.B;

        return brightness > 0.85f ? Color.Black.ToHex() : Color.White.ToHex();
    }

    [ViewVariables]
    public new string Text
    {
        get => Label.Text ?? string.Empty;
        set
        {
            var markup = MakeTextReadable(value, ModulateSelfOverride ?? Modulate);
            Label.SetMessage(FormattedMessage.FromMarkupPermissive(markup), TagsAllowed);
        }
    }
}
