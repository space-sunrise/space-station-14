using System.Linq;

namespace Content.Shared._Sunrise.ExtendedColor;

[Serializable]
public sealed class ExtendedColor
{
    public ColorType Type;
    public Dictionary<string, Color> Colors;

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

    public ExtendedColor(ColorType type, Dictionary<string, Color> colors)
    {
        Type = type;
        Colors = colors;
    }

    public Color GetColor(string key, Color? def = null)
    {
        def ??= Color.White;

        return Colors.TryGetValue(key, out var col) ? col : def.Value;
    }

    public static ExtendedColor White => new(Color.White);

    public override string ToString()
    {
        var colorPairs = Colors.Select(kvp => $"{kvp.Key}={kvp.Value.ToHex()}");
        return $"{Type}:{string.Join(",", colorPairs)}";
    }

    public static ExtendedColor? FromString(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return null;

        var parts = str.Split(':', 2);
        if (parts.Length != 2)
            return null;

        if (!Enum.TryParse<ColorType>(parts[0], out var type))
            return null;

        var dict = new Dictionary<string, Color>();
        var entries = parts[1].Split(',');

        foreach (var entry in entries)
        {
            var kv = entry.Split('=', 2);
            if (kv.Length != 2)
                continue;

            var key = kv[0];
            var color = Color.FromHex(kv[1]);
            dict[key] = color;
        }

        return new ExtendedColor(type, dict);
    }

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
}
