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



    #region Parsing

    public static Vector2 ParseVector2(string str)
    {
        str = str.Trim('(', ')');
        var parts = str.Split(',');

        if (parts.Length != 2
            || !float.TryParse(parts[0], CultureInfo.InvariantCulture, out var x)
            || !float.TryParse(parts[1], CultureInfo.InvariantCulture, out var y))
            return new(0, 0);

        return new Vector2(x, y);
    }

    public override string ToString()
    {
        var lines = new Dictionary<string, string>();

        lines.Add("type", Type.ToString());
        lines.Add("offset", Offset.ToString());
        lines.Add("size", Size.ToString());
        lines.Add("rotation", Rotation.ToString(CultureInfo.InvariantCulture));
        lines.Add("speed", Speed.ToString(CultureInfo.InvariantCulture));

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
                default:
                {
                    if (key.StartsWith("color."))
                        colors[key["color.".Length..]] = Color.TryFromHex(val) ?? Color.White;
                    break;
                }
            }
        }

        return new ExtendedColor(type, colors, offset, size, rotation, speed);
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
        float speed)
    {
        Type = type;
        Colors = colors;
        Offset = offset;
        Size = size;
        Rotation = rotation;
        Speed = speed;
    }
    #endregion

    public Color GetColor(string key, Color? def = null)
    {
        def ??= Color.White;

        return Colors.TryGetValue(key, out var col) ? col : def.Value;
    }

    public static ExtendedColor White => new(Color.White);

    public bool Equals(ExtendedColor? other)
    {
        return Type.Equals(other?.Type)
            && Colors.Equals(other?.Colors);
    }
}

public enum ColorType
{
    Color,
    Gradient,
    SelectionOutline
}
