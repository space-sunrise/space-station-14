namespace Content.Client._Sunrise.UserInterface;

public static class UiTextHelper
{
    private const string Ellipsis = "...";

    public static string TruncateWithEllipsis(string? input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || maxLength <= 0)
            return string.Empty;

        if (input.Length <= maxLength)
            return input;

        if (maxLength <= Ellipsis.Length)
            return Ellipsis.AsSpan(0, maxLength).ToString();

        var cutLength = maxLength - Ellipsis.Length;
        return string.Concat(input.AsSpan(0, cutLength), Ellipsis);
    }
}
