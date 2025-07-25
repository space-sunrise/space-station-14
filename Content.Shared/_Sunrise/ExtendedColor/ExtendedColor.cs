using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace Content.Shared._Sunrise.ExtendedColor;

[Serializable]
public sealed class ExtendedColor
{
    public ColorType Type;
    public Dictionary<string, Color> Colors;

    public Vector2 Offset = new(0, 0);
    public Vector2 Size = new(1, 1);
    public float Rotation = 0;
    public float Speed = 1;
    public bool Pixelated = true;
    public bool Mirrored = false;



    #region Parsing

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

    public string Vector2ToString(Vector2 v)
    {
        return $"({v.X.ToString(CultureInfo.InvariantCulture)}={v.Y.ToString(CultureInfo.InvariantCulture)})";
    }

    public override string ToString()
    {
        var lines = new Dictionary<string, string>();

        lines.Add("type", Type.ToString());
        lines.Add("offset", Vector2ToString(Offset));
        lines.Add("size", Vector2ToString(Size));
        lines.Add("rotation", Rotation.ToString(CultureInfo.InvariantCulture));
        lines.Add("speed", Speed.ToString(CultureInfo.InvariantCulture));
        lines.Add("pixelated", Pixelated.ToString());
        lines.Add("mirrored", Mirrored.ToString());

        foreach (var (k, v) in Colors)
            lines.Add($"color.{k}", $"{v.ToHex()}");

        return string.Join(",", lines.Select(kvp => $"{kvp.Key}={kvp.Value}"));
    }

    public static ExtendedColor? FromString(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return null;

        var lines = str.Split(',');
        ColorType type = ColorType.Color;
        var colors = new Dictionary<string, Color>();

        var offset = new Vector2(0, 0);
        var size = new Vector2(0, 0);
        var rotation = 0f;
        var speed = 1f;
        var pixelated = true;
        var mirrored = false;

        foreach (var line in lines)
        {
            var parts = line.Split('=', 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            var val = parts[1].Trim();

            switch (key)
            {
                case "type" when Enum.TryParse(val, out ColorType parsedType):
                    type = parsedType;
                    break;
                case "offset":
                    offset = ParseVector2(val);
                    break;
                case "size":
                    size = ParseVector2(val);
                    break;
                case "rotation" when float.TryParse(val, CultureInfo.InvariantCulture, out rotation):
                case "speed" when float.TryParse(val, CultureInfo.InvariantCulture, out speed):
                    break;
                case "pixelated":
                    pixelated = val == "True";
                    break;
                case "mirrored":
                    mirrored = val == "True";
                    break;
                default:
                {
                    if (key.StartsWith("color."))
                        colors[key["color.".Length..]] = Color.TryFromHex(val) ?? Color.White;
                    break;
                }
            }
        }

        return new ExtendedColor(type, colors, offset, size, rotation, speed, pixelated, mirrored);
    }
    #endregion


    #region Constructors
    public ExtendedColor()
    {
        Type = ColorType.Color;
        Colors = new Dictionary<string, Color>()
        {
            { "base", Color.Black}
        };
    }

    public ExtendedColor(Color color)
    {
        Type = ColorType.Color;
        Colors = new Dictionary<string, Color>
        {
            {"base", color }
        };
    }

    public ExtendedColor(ColorType type,
        Dictionary<string, Color> colors,
        Vector2 offset,
        Vector2 size,
        float rotation,
        float speed,
        bool pixelated,
        bool mirrored)
    {
        Type = type;
        Colors = colors;
        Offset = offset;
        Size = size;
        Rotation = rotation;
        Speed = speed;
        Pixelated = pixelated;
        Mirrored = mirrored;
    }
    #endregion

    public Color GetColor(string key, Color? def = null)
    {
        def ??= Color.White;

        return Colors.TryGetValue(key, out var col) ? col : def.Value;
    }

    public static ExtendedColor White => new(Color.White);

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

    public ExtendedColor Clone()
    {
        return new ExtendedColor(Type, new(Colors), new(Offset.X, Offset.Y), new(Size.X, Size.Y), Rotation, Speed, Pixelated, Mirrored);
    }

    public bool Equals(ExtendedColor? other)
    {
        if (other == null)
            return false;

        return Type.Equals(other?.Type)
               && DictionaryEquals(Colors, other?.Colors)
               && Offset.Equals(other?.Offset)
               && Size.Equals(other?.Size)
               && Rotation.Equals(other?.Rotation)
               && Speed.Equals(other?.Speed)
               && Pixelated.Equals(other?.Pixelated)
               && Mirrored.Equals(other?.Mirrored);
    }
}

public enum ColorType
{
    Color,
    Gradient,
    SelectionOutline
}
