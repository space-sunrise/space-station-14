using System.Linq;
using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.MarkingEffects;

[Serializable, NetSerializable]
public sealed partial class RoughGradientMarkingEffect : MarkingEffect
{
    public override MarkingEffectType Type => MarkingEffectType.RoughGradient;

    public bool Horizontal = false;

    #region Parsing
    public override string ToString()
    {
        var dict = new Dictionary<string, string>();

        dict.Add("horizontal", ParamToString(Horizontal));

        foreach (var (k, v) in Colors)
            dict.Add($"color.{k}", $"{v.ToHex()}");

        var result = string.Join(",", dict.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{Type.ToString()}=={result}";
    }

    public static RoughGradientMarkingEffect? Parse(Dictionary<string, string> dict)
    {
        var colors = new Dictionary<string, Color>();

        var horizontal = false;

        foreach (var (type, value) in dict)
        {
            switch (type)
            {
                case "horizontal":
                    TryParseParam(value, out horizontal);
                    break;
                default:
                {
                    if (type.StartsWith("color."))
                        colors[type["color.".Length..]] = Color.TryFromHex(value) ?? Color.White;
                    break;
                }
            }
        }

        return new RoughGradientMarkingEffect(colors, horizontal);
    }
    #endregion

    #region Constructors
    public RoughGradientMarkingEffect()
    {
        Colors = new Dictionary<string, Color>()
        {
            { "base", Color.White }
        };
    }

    public RoughGradientMarkingEffect(Color color)
    {
        Colors = new Dictionary<string, Color>
        {
            {"base", color }
        };
    }

    public RoughGradientMarkingEffect(Dictionary<string, Color> colors, bool horizontal)
    {
        Colors = colors;
        Horizontal = horizontal;
    }
    #endregion

    #region Other methods
    public override RoughGradientMarkingEffect Clone()
    {
        return new RoughGradientMarkingEffect(new(Colors), Horizontal);
    }

    public override bool Equals(MarkingEffect? maybeOther)
    {
        if (maybeOther is not RoughGradientMarkingEffect other)
            return false;

        return DictionaryEquals(Colors, other.Colors)
            && Equals(Horizontal, other.Horizontal);
    }
    #endregion
}
