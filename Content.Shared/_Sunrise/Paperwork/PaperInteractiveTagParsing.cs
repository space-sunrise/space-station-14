using System.Text.RegularExpressions;

namespace Content.Shared._Sunrise.Paperwork;

/// <summary>
/// Нужен для поиска в тексте интерактивных плейсхолдеров.
/// Используется для замены плейсхоледера на нужный текст
/// </summary>
public static class PaperInteractiveTagParsing
{
    public const string SignatureTagName = "signature";
    public const string FormTagName = "form";
    public const string JobTagName = "job";

    // поддержка вот такого -> [signature="имя и фамилия"].
    public const string SignatureTagRegexPattern = @"(?<!\\)\[signature(?<attrs>[^\]]*)\]";
    public const string FormTagRegexPattern = @"(?<!\\)\[form(?<attrs>[^\]]*)\]";
    public const string JobTagRegexPattern = @"(?<!\\)\[job(?<attrs>[^\]]*)\]";

    public static readonly Regex SignatureTagRegex =
        new(SignatureTagRegexPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static readonly Regex FormTagRegex =
        new(FormTagRegexPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static readonly Regex JobTagRegex =
        new(JobTagRegexPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool ContainsInteractiveTags(string text)
    {
        return SignatureTagRegex.IsMatch(text) || FormTagRegex.IsMatch(text) || JobTagRegex.IsMatch(text);
    }

    public static string? ReplaceNthTag(string text, Regex regex, int index, string replacement)
    {
        if (index < 0)
            return null;

        var matches = regex.Matches(text);
        if (index >= matches.Count)
            return null;

        var match = matches[index];
        return text.Remove(match.Index, match.Length).Insert(match.Index, replacement);
    }
}
