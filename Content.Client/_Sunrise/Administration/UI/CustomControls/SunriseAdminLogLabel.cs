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

    private static readonly Regex LogHighlightRegex = new(
        @"(\bEntId=\d+\b|(?<=\()(\b\d+/[^\)]+|\bn\d+\b)(?=\)))|\b(dropped|picked up|inserted|removed|equipped|unequipped|thrown|wielded|unwielded|loaded|unloaded|spawned|deleted|shot|attacked|damaged|hit|exploded|fired|clicked|knocked down|activated|interacted|opened|closed|locked|unlocked|anchored|unanchored|welded|unwelded|bolted|unbolted|connected|disconnected|joined|left|banned|kicked|suicided|died|revived|cloned|respawned|joined|left|refilled|drained|poured|ingested|vomited|collapsed|unconscious|rejuvenated|mounted|dismounted)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private const string VerbColor = "#36A3D9"; // Cyan-ish

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
        
        return LogHighlightRegex.Replace(strippedMessage, m =>
        {
            // If it's an ID (contains digit/slash or EntId)
            if (m.Value.Contains('(') || m.Value.Contains('=') || (m.Value.Length > 0 && char.IsDigit(m.Value[0])))
                return $"[color={InfoColor}]{m.Value}[/color]";

            // Otherwise it's a verb
            return $"[color={VerbColor}]{m.Value}[/color]";
        });
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
        LogImpact.High => "#FFA500", // Orange hex to be safe
        LogImpact.Medium => "yellow",
        LogImpact.Low => "green",

        _ => "blue",
    };


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
