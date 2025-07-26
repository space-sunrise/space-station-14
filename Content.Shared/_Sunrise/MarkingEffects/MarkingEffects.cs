using System.Globalization;
using System.Linq;
using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.MarkingEffects;

public enum MarkingEffectType
{
    Color,
    Gradient,
}

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

        var lines = input.Split(',');

        if (lines.Length == 0)
            return null;

        return lines
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
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

        switch (type)
        {
            case MarkingEffectType.Color:
                return ColorMarkingEffect.Parse(dict);
            case MarkingEffectType.Gradient:
                return GradientMarkingEffect.Parse(dict);
        }

        return null;
    }

    public static string ParamToString<T>(T param)
    {
        if (param == null)
            return "";

        if (typeof(T) == typeof(float))
            return ((float)(object)param).ToString(CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(bool))
            return ((bool)(object)param).ToString();
        if (typeof(T) == typeof(Vector2))
            return Vector2ToString((Vector2)(object)param);

        return "";
    }

    public static bool TryParseParam<T>(string input, out T param)
    {
        param = default!;

        if (typeof(T) == typeof(float))
        {
            if (!float.TryParse(input, CultureInfo.InvariantCulture, out var result))
                return false;

            param = (T)(object)result;
        }
        else if (typeof(T) == typeof(Vector2))
            param = (T)(object)ParseVector2(input);
        else if (typeof(T) == typeof(bool))
            param = (T)(object)(input == "True");
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

[Serializable, NetSerializable]
public sealed partial class ColorMarkingEffect : MarkingEffect
{
    public override MarkingEffectType Type => MarkingEffectType.Color;

    public Color GetColor()
        => Colors.TryGetValue("base", out var col) ? col : Color.White;

    #region Constructors

    public ColorMarkingEffect(Color color) : base(color) { }
    public static ColorMarkingEffect White => new(Color.White);

    #endregion

    #region Parsers

    public override string ToString()
    {
        Dictionary<string, string> dict = new();

        var color = GetColor();
        dict.Add($"color.base", color.ToHex());

        var result = string.Join(",", dict.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{Type.ToString()}=={result}";
    }

    public static ColorMarkingEffect? Parse(Dictionary<string, string> dict)
    {
        var color = Color.White;

        foreach (var (type, value) in dict)
        {
            if (type == "color.base")
                color = Color.TryFromHex(value) ?? Color.White;
        }

        return new ColorMarkingEffect(color);
    }

    public override ColorMarkingEffect Clone()
    {
        return new ColorMarkingEffect(Colors["base"]);
    }

    public override bool Equals(MarkingEffect? maybeOther)
    {
        if (maybeOther is not ColorMarkingEffect other)
            return false;

        return DictionaryEquals(Colors, other.Colors);
    }

    #endregion
}

[Serializable, NetSerializable]
public sealed partial class GradientMarkingEffect : MarkingEffect
{
    public override MarkingEffectType Type => MarkingEffectType.Gradient;

    public Vector2 Offset = new(0, 0);
    public Vector2 Size = new(1, 1);
    public float Rotation = 0;
    public float Speed = 1;
    public bool Pixelated = true;
    public bool Mirrored = false;

    #region Parsing
    public override string ToString()
    {
        var dict = new Dictionary<string, string>();

        dict.Add("offset", ParamToString(Offset));
        dict.Add("size", ParamToString(Size));
        dict.Add("rotation", ParamToString(Rotation));
        dict.Add("speed", ParamToString(Speed));
        dict.Add("pixelated", ParamToString(Pixelated));
        dict.Add("mirrored", ParamToString(Mirrored));

        foreach (var (k, v) in Colors)
            dict.Add($"color.{k}", $"{v.ToHex()}");

        var result = string.Join(",", dict.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{Type.ToString()}=={result}";
    }

    public static GradientMarkingEffect? Parse(Dictionary<string, string> dict)
    {
        var colors = new Dictionary<string, Color>();

        var offset = new Vector2(0, 0);
        var size = new Vector2(1, 1);
        var rotation = 0f;
        var speed = 1f;
        var pixelated = true;
        var mirrored = false;

        foreach (var (type, value) in dict)
        {
            switch (type)
            {
                case "offset":
                    TryParseParam(value, out offset);
                    break;
                case "size":
                    TryParseParam(value, out size);
                    break;
                case "rotation":
                    TryParseParam(value, out rotation);
                    break;
                case "speed":
                    TryParseParam(value, out speed);
                    break;
                case "pixelated":
                    TryParseParam(value, out pixelated);
                    break;
                case "mirrored":
                    TryParseParam(value, out mirrored);
                    break;
                default:
                {
                    if (type.StartsWith("color."))
                        colors[type["color.".Length..]] = Color.TryFromHex(value) ?? Color.White;
                    break;
                }
            }
        }

        return new GradientMarkingEffect(colors, offset, size, rotation, speed, pixelated, mirrored);
    }
    #endregion


    #region Constructors
    public GradientMarkingEffect()
    {
        Colors = new Dictionary<string, Color>()
        {
            { "base", Color.Black}
        };
    }

    public GradientMarkingEffect(Color color)
    {
        Colors = new Dictionary<string, Color>
        {
            {"base", color }
        };
    }

    public GradientMarkingEffect(Dictionary<string, Color> colors,
        Vector2 offset,
        Vector2 size,
        float rotation,
        float speed,
        bool pixelated,
        bool mirrored)
    {
        Colors = colors;
        Offset = offset;
        Size = size;
        Rotation = rotation;
        Speed = speed;
        Pixelated = pixelated;
        Mirrored = mirrored;
    }
    #endregion



    public override GradientMarkingEffect Clone()
    {
        return new GradientMarkingEffect(new(Colors), new(Offset.X, Offset.Y), new(Size.X, Size.Y), Rotation, Speed, Pixelated, Mirrored);
    }

    public override bool Equals(MarkingEffect? maybeOther)
    {
        if (maybeOther is not GradientMarkingEffect other)
            return false;

        return DictionaryEquals(Colors, other.Colors)
               && Offset.Equals(other.Offset)
               && Size.Equals(other.Size)
               && Rotation.Equals(other.Rotation)
               && Speed.Equals(other.Speed)
               && Pixelated.Equals(other.Pixelated)
               && Mirrored.Equals(other.Mirrored);
    }
}
