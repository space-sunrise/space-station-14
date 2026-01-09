using System.Text.RegularExpressions;
using Content.Client.Administration.UI.CustomControls;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Administration.UI.CustomControls;

/// <summary>
/// Красивая обертка для логов, поддерживающая форматирование текста.
/// </summary>
/// <remarks>
/// Добавляет в начало файла цветовой индикатор важности логи и выделяет время жирным текстом. <br/>
/// Дополнительную информацию о сущности выделяет <see cref="InfoColor"/> цветом, чтобы не засорять основную информацию. <br/>
/// Цвета логов определяются в <see cref="GetTypeSpecificColor"/>
/// </remarks>
/// <seealso cref="AdminLogLabel"/>
public sealed class SunriseAdminLogLabel : RichTextLabel
{
    private const string InfoColor = "gray";

    private static readonly Regex TagRegex = new(
        @"\[(\/?)[a-zA-Z0-9]+(=[^\]]+)?\]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly Regex BracketRegex = new(
        @"[\[\]]",
        RegexOptions.Compiled
    );

    public SunriseAdminLogLabel(ref SharedAdminLog log, HSeparator separator)
    {
        Log = log;
        Separator = separator;

        var formatted = new FormattedMessage(10);

        var formattedTime = GetFormattedTime(ref log);
        var formattedMessage = GetFormattedLogMessage(ref log);

        formatted.AddMarkupOrThrow($"{formattedTime}: {formattedMessage}");
        formatted.Pop();

        SetMessage(formatted);

        OnVisibilityChanged += VisibilityChanged;
    }

    private static string GetFormattedTime(ref SharedAdminLog log)
    {
        var marker = GenerateImpactMarker(ref log);
        return $"[bold]{marker} {log.Date:HH:mm:ss}[/bold]";
    }

    private static string GetFormattedLogMessage(ref SharedAdminLog log)
    {
        var strippedMessage = StripTagsAndBrackets(log.Message);
        return AddColorTagsToParentheses(strippedMessage);
    }

    private static string GenerateImpactMarker(ref SharedAdminLog log)
    {
        var color = GetTypeSpecificColor(log.Impact);
        return $"[color={color}]|!|[/color]";
    }

    #region Helpers

    private static string GetTypeSpecificColor(LogImpact type) => type switch
    {
        LogImpact.Extreme => "red",
        LogImpact.High => "orange",
        LogImpact.Medium => "yellow",
        LogImpact.Low => "green",

        _ => "blue",
    };

    private static string AddColorTagsToParentheses(string inputText)
    {
        if (string.IsNullOrEmpty(inputText))
            return inputText;

        return inputText.Replace("(", $"[color={InfoColor}](")
            .Replace(")", ")[/color]");
    }

    private static string StripTagsAndBrackets(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var noTags = TagRegex.Replace(input, string.Empty);

        return BracketRegex.Replace(noTags, string.Empty);
    }

    #endregion

    public new SharedAdminLog Log { get; }

    public HSeparator Separator { get; }

    private void VisibilityChanged(Control control)
    {
        Separator.Visible = Visible;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        OnVisibilityChanged -= VisibilityChanged;
    }
}
