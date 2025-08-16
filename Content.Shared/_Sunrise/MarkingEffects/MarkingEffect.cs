using System.Globalization;
using System.Linq;
using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.MarkingEffects;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class MarkingEffect
{
    public abstract MarkingEffectType Type { get; }
    public Dictionary<string, Color> Colors;

    public abstract override string ToString();
    public abstract MarkingEffect Clone();
    public abstract bool Equals(MarkingEffect? other);

    #region Constructors

    protected MarkingEffect()
    {
        Colors = new Dictionary<string, Color>
        {
            { "base", Color.White }
        };
    }

    protected MarkingEffect(Color color)
    {
        Colors = new Dictionary<string, Color>
        {
            { "base", color }
        };
    }

    #endregion

    #region Parsers

    protected static Dictionary<string, string>? ParseToDict(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var spl = input.Split("==");
        if (spl.Length > 1)
            input = spl[1];

        var lines = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 0)
            return null;

        return lines
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
            .ToDictionary(parts => parts[0], parts => parts[1]);
    }

    public static MarkingEffect? Parse(string input)
    {
        var pair = input.Split("==");

        if (pair.Length != 2 || !Enum.TryParse<MarkingEffectType>(pair[0], true, out var type))
            return null;

        var dict = ParseToDict(input);

        if (dict == null)
            return null;

        return MarkingEffectTypes.TypeParsers.TryGetValue(type, out var parser) ? parser(dict) : null;
    }

    public static string ParamToString<T>(T param)
    {
        if (param == null)
            return "";

        return param switch
        {
            null => "",
            float f => f.ToString(CultureInfo.InvariantCulture),
            bool b => b.ToString(),
            Vector2 v => Vector2ToString(v),
            _ => "",
        };
    }

    public static bool TryParseParam<T>(string input, out T param)
    {
        param = default!;

        if (typeof(T) == typeof(float))
        {
            if (!float.TryParse(input, CultureInfo.InvariantCulture, out var floatResult))
                return false;

            param = (T)(object)floatResult;
        }
        else if (typeof(T) == typeof(bool))
        {
            if (!bool.TryParse(input, out var boolResult))
                return false;

            param = (T)(object)boolResult;
        }
        else if (typeof(T) == typeof(Vector2))
            param = (T)(object)ParseVector2(input);
        else
            return false;

        return true;
    }
    #endregion

    #region Static methods

    public static Vector2 ParseVector2(string str)
    {
        str = str.Trim('(', ')');
        var parts = str.Split('=');

        if (parts.Length != 2
            || !float.TryParse(parts[0], CultureInfo.InvariantCulture, out var x)
            || !float.TryParse(parts[1], CultureInfo.InvariantCulture, out var y))
            return new(0, 0);

        return new Vector2(x, y);
    }

    public static bool DictionaryEquals<TKey, TValue>(
        Dictionary<TKey, TValue>? a,
        Dictionary<TKey, TValue>? b)
        where TKey : notnull
    {
        if (a == b)
            return true;
        if (a == null || b == null)
            return false;

        return a.Count == b.Count && !a.Except(b).Any();
    }

    public static string Vector2ToString(Vector2 v)
    {
        return $"({v.X.ToString(CultureInfo.InvariantCulture)}={v.Y.ToString(CultureInfo.InvariantCulture)})";
    }
    #endregion
}
