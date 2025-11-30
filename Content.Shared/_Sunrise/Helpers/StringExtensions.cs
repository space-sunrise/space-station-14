using System.Text;
using System.Text.RegularExpressions;

namespace Content.Shared._Sunrise.Helpers;

public static class StringExtensions
{
    private static readonly Regex AllowedCharsRegex = new Regex(
        @"[^a-zA-Zа-яА-ЯёЁ0-9\s.,!?;:\-_\(\)\[\]{}""'/\\@#%\^&\*\+=<>]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    /// <summary>
    /// Санитизация пользовательского ввода:
    /// - Убирает лишние пробелы по краям
    /// - Обрезает до maxLength (если задан)
    /// - Убирает недопустимые символы
    /// - Нормализует Unicode
    /// </summary>
    /// <remarks>
    /// Рекомендуется для UI, содержащих LineEdit или подобные возможности передать текст на сервер.
    /// </remarks>
    public static string SanitizeInput(this string? input, int? maxLength = null)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        input = input.Trim();

        if (maxLength.HasValue && input.Length > maxLength.Value)
            input = input.Substring(0, maxLength.Value);

        input = input.Normalize(NormalizationForm.FormC);

        input = AllowedCharsRegex.Replace(input, string.Empty);

        return input;
    }
}
